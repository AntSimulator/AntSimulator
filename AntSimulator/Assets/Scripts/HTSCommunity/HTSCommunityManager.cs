using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HTSCommunityManager : MonoBehaviour
{
    [Header("Templates (ScriptableObject)")]
    public List<CommunityPostSO> templates = new();

    [Header("Daily Base (per stock)")]
    public int basePostsPerDayPerStock = 1;     // 평소 하루 1개 정도
    public bool scheduleBasePosts = true;

    [Header("Burst Trigger")]
    [Tooltip("틱 등락률 절대값이 이 값 이상이면 글 폭증 후보")]
    public float moveBurstThreshold = 0.02f;    // 2%
    [Tooltip("tickVol / emaVol 이 값 이상이면 거래량 폭증 후보")]
    public float volumeBurstRelThreshold = 2.5f;
    [Tooltip("volatilityMultiplier 이 값 이상이면 폭증 후보")]
    public float volMulBurstThreshold = 1.6f;

    [Header("Burst Count")]
    public int burstMin = 1;
    public int burstMax = 6;

    [Header("Timing")]
    public int ticksPerDay = 300;
    public int morningTick = 0;

    [Header("Debug")]
    public bool logWhenPosted = true;

    // ====== Output ======
    // stockId별 게시글 리스트
    private readonly Dictionary<string, List<CommunityPost>> postsByStock = new();

    // UI 붙일 때 편한 이벤트
    public event Action<string, CommunityPost> OnStockPostCreated;

    // 오늘의 예약 스케줄
    private readonly List<Scheduled> schedule = new();
    private int todayDay = -1;

    [Serializable]
    private class Scheduled
    {
        public int day;
        public int tickInDay;
        public string stockId;
        public CommunityPostType type;
        public string linkedEventId;   // 필요하면 나중에 붙이기
    }

    // ---- 외부에서 초기 종목 등록할 때 ----
    public void InitStocks(IEnumerable<string> stockIds)
    {
        postsByStock.Clear();
        schedule.Clear();
        todayDay = -1;

        if (stockIds == null) return;

        foreach (var id in stockIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            postsByStock[id] = new List<CommunityPost>();
        }
    }

    // ---- 외부(UI)가 읽기 ----
    public IReadOnlyList<CommunityPost> GetPosts(string stockId)
    {
        if (string.IsNullOrEmpty(stockId)) return Array.Empty<CommunityPost>();
        return postsByStock.TryGetValue(stockId, out var list) ? list : Array.Empty<CommunityPost>();
    }

    // ---- 하루 시작: “기본 글” 예약 ----
    public void OnDayStarted(int day)
    {
        todayDay = day;
        schedule.Clear();

        if (!scheduleBasePosts) return;

        foreach (var stockId in postsByStock.Keys.ToList())
        {
            for (int i = 0; i < basePostsPerDayPerStock; i++)
            {
                AddSchedule(new Scheduled
                {
                    day = day,
                    tickInDay = RandomTickAvoidMorning(),
                    stockId = stockId,
                    type = CommunityPostType.Meme // 기본은 밈/잡담 느낌(원하면 Info로)
                });
            }
        }

        schedule.Sort((a, b) => a.tickInDay.CompareTo(b.tickInDay));
        if (logWhenPosted) Debug.Log($"[HTSCommunity] Day{day} scheduled base posts: {schedule.Count}");
    }

    // ---- 틱마다 호출: 1) 예약 글 처리 2) 급변하면 즉시 글 생성 ----
    public void OnTick(
        int day,
        int tickInDay,
        string stockId,
        float prevPrice,
        float currentPrice,
        long tickVolume,
        float emaVolume,
        float volatilityMultiplier,
        bool hasEventEffect = false,
        float communitySensitivity = 1f
    )
    {
        if (todayDay != day) OnDayStarted(day);

        // 1) 예약 글 처리
        PumpScheduled(day, tickInDay);

        // 2) 종목별 급변 체크 → 버스트 글 생성
        if (string.IsNullOrEmpty(stockId)) return;
        if (!postsByStock.ContainsKey(stockId)) return;

        float retAbs = Mathf.Abs((currentPrice - prevPrice) / Mathf.Max(0.0001f, prevPrice));
        float relVol = (emaVolume <= 1f) ? 1f : (tickVolume / Mathf.Max(1f, emaVolume));

        bool burst =
            retAbs >= moveBurstThreshold ||
            relVol >= volumeBurstRelThreshold ||
            volatilityMultiplier >= volMulBurstThreshold ||
            hasEventEffect;

        if (!burst) return;

        int count = CalcBurstCount(retAbs, relVol, volatilityMultiplier, hasEventEffect, communitySensitivity);
        for (int i = 0; i < count; i++)
        {
            var type = PickBurstType(retAbs, hasEventEffect);
            CreateAndAddPost(stockId, day, tickInDay, type, linkedEventId: null);
        }
    }

    // ===================== 내부 =====================

    private void PumpScheduled(int day, int tickInDay)
    {
        for (int i = schedule.Count - 1; i >= 0; i--)
        {
            var s = schedule[i];
            if (s.day != day) continue;
            if (s.tickInDay != tickInDay) continue;

            CreateAndAddPost(s.stockId, s.day, s.tickInDay, s.type, s.linkedEventId);
            schedule.RemoveAt(i);
        }
    }

    private void CreateAndAddPost(string stockId, int day, int tickInDay, CommunityPostType type, string linkedEventId)
    {
        var template = PickTemplateByType(type);
        if (template == null) return;

        var post = new CommunityPost
        {
            id = Guid.NewGuid().ToString("N"),
            day = day,
            tickInDay = tickInDay,
            type = template.type,
            title = template.title,
            body = template.body,
            relatedStockId = template.allowAttachStockId ? stockId : null,
            linkedEventId = template.allowAttachEventId ? linkedEventId : null,
            templateId = template.templateId
        };

        postsByStock[stockId].Add(post);
        OnStockPostCreated?.Invoke(stockId, post);

        if (logWhenPosted)
            Debug.Log($"[HTSCommunity][{stockId}][D{day} T{tickInDay}] {post.type} | {post.title}");
    }

    private CommunityPostSO PickTemplateByType(CommunityPostType type)
    {
        if (templates == null || templates.Count == 0) return null;

        var list = templates.Where(t => t != null && t.type == type).ToList();
        if (list.Count == 0) return null;

        float total = 0f;
        for (int i = 0; i < list.Count; i++) total += Mathf.Max(0.001f, list[i].weight);

        float r = UnityEngine.Random.Range(0f, total);
        float acc = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            acc += Mathf.Max(0.001f, list[i].weight);
            if (r <= acc) return list[i];
        }
        return list[list.Count - 1];
    }

    private CommunityPostType PickBurstType(float retAbs, bool hasEventEffect)
    {
        // 이벤트 영향 있으면 Reaction 쪽으로
        if (hasEventEffect) return CommunityPostType.EventReaction;

        // 급락/급등은 Loss/Brag로 나뉘게 하고 싶으면 여기서 확장 가능
        // 지금은 “움직임이 크면 잡담+밈+정보 섞기”
        float r = UnityEngine.Random.value;
        if (r < 0.45f) return CommunityPostType.Meme;
        if (r < 0.70f) return CommunityPostType.Info;
        if (r < 0.85f) return CommunityPostType.Brag;
        return CommunityPostType.Loss;
    }

    private int CalcBurstCount(float retAbs, float relVol, float volMul, bool hasEvent, float sensitivity)
    {
        // 대충 “움직임 + 거래량”을 점수로 만들고 그걸 글 수로 변환
        float score = 0f;
        score += Mathf.InverseLerp(moveBurstThreshold, 0.10f, retAbs);     // 2%~10%
        score += Mathf.InverseLerp(volumeBurstRelThreshold, 6f, relVol);  // 2.5배~6배
        score += Mathf.InverseLerp(volMulBurstThreshold, 3.0f, volMul);   // 1.6~3.0
        if (hasEvent) score += 0.5f;

        score *= Mathf.Max(0.1f, sensitivity);

        int c = Mathf.RoundToInt(Mathf.Lerp(burstMin, burstMax, Mathf.Clamp01(score)));
        return Mathf.Clamp(c, burstMin, burstMax);
    }

    private void AddSchedule(Scheduled s)
    {
        s.tickInDay = Mathf.Clamp(s.tickInDay, 0, Mathf.Max(0, ticksPerDay - 1));
        schedule.Add(s);
    }

    private int RandomTickAvoidMorning()
    {
        int tpd = Mathf.Max(1, ticksPerDay);
        if (tpd <= 1) return 0;

        int t = UnityEngine.Random.Range(0, tpd);
        if (t == morningTick) t = (t + 1) % tpd;
        return t;
    }
    public List<CommunityPost> GetPostsForStock(string stockId)
    {
        if (!postsByStock.ContainsKey(stockId)) return new List<CommunityPost>();
        return postsByStock[stockId];
    }
}