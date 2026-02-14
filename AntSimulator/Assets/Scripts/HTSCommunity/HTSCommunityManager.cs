using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HTSCommunityManager : MonoBehaviour
{
    [Header("Templates (ScriptableObject)")]
    public List<HTSCommunitySO> templates = new();

    [Header("Daily Base (per stock)")]
    public int basePostsPerDayPerStock = 1; // 평소 하루 1개 정도
    public bool scheduleBasePosts = true;

    [Header("Burst Trigger")]
    [Tooltip("틱 등락률 절대값이 이 값 이상이면 글 폭증 후보")]
    public float moveBurstThreshold = 0.02f; // 2%
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
    private readonly Dictionary<string, List<HTSPost>> postsByStock = new();

    // UI 이벤트
    public event Action<string, HTSPost> OnStockPostCreated;

    // 오늘의 예약 스케줄
    private readonly List<Scheduled> schedule = new();
    private int todayDay = -1;

    [Serializable]
    private class Scheduled
    {
        public int day;
        public int tickInDay;
        public string stockId;
        public HTSReactionDirection direction;
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
            postsByStock[id] = new List<HTSPost>();
        }
    }

    // ---- 외부(UI)가 읽기 ----
    public IReadOnlyList<HTSPost> GetPosts(string stockId)
    {
        if (string.IsNullOrEmpty(stockId)) return Array.Empty<HTSPost>();
        return postsByStock.TryGetValue(stockId, out var list) ? list : Array.Empty<HTSPost>();
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
                    direction = UnityEngine.Random.value < 0.5f ? HTSReactionDirection.Up : HTSReactionDirection.Down
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

        // 상승/하락 방향은 가격으로 결정 (동일하면 Up 처리)
        var direction = (currentPrice >= prevPrice) ? HTSReactionDirection.Up : HTSReactionDirection.Down;

        for (int i = 0; i < count; i++)
        {
            CreateAndAddPost(stockId, day, tickInDay, direction);
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

            CreateAndAddPost(s.stockId, s.day, s.tickInDay, s.direction);
            schedule.RemoveAt(i);
        }
    }

    private void CreateAndAddPost(string stockId, int day, int tickInDay, HTSReactionDirection direction)
    {
        var template = PickTemplate(stockId, direction);
        if (template == null) return;

        var post = new HTSPost
        {
            id = Guid.NewGuid().ToString("N"),
            day = day,
            tickInDay = tickInDay,
            stockId = stockId,
            direction = direction,
            title = template.title,
            body = template.body
        };

        if (!postsByStock.ContainsKey(stockId))
            postsByStock[stockId] = new List<HTSPost>();

        postsByStock[stockId].Add(post);
        OnStockPostCreated?.Invoke(stockId, post);

        if (logWhenPosted)
            Debug.Log($"[HTSCommunity][{stockId}][D{day} T{tickInDay}] {direction} | {post.title}");
    }

    private HTSCommunitySO PickTemplate(string stockId, HTSReactionDirection direction)
    {
        if (templates == null || templates.Count == 0) return null;

        // 1) 해당 stockId 전용 템플릿 우선
        var exact = templates
            .Where(t => t != null
                        && t.direction == direction
                        && string.Equals(t.stockId, stockId, StringComparison.Ordinal))
            .ToList();

        Debug.Log($"[PickTemplate] stock={stockId} dir={direction} exact={exact.Count}");
        
        if (exact.Count > 0)
            return exact[UnityEngine.Random.Range(0, exact.Count)];

        // 2) 공용 템플릿(빈 stockId) fallback
        var generic = templates
            .Where(t => t != null
                        && t.direction == direction
                        && string.IsNullOrEmpty(t.stockId))
            .ToList();
        
        Debug.Log($"[PickTemplate] stock={stockId} dir={direction} generic={generic.Count}");

        if (generic.Count > 0)
            return generic[UnityEngine.Random.Range(0, generic.Count)];

        return null;
    }

    private int CalcBurstCount(float retAbs, float relVol, float volMul, bool hasEvent, float sensitivity)
    {
        float score = 0f;
        score += Mathf.InverseLerp(moveBurstThreshold, 0.10f, retAbs);      // 2%~10%
        score += Mathf.InverseLerp(volumeBurstRelThreshold, 6f, relVol);   // 2.5배~6배
        score += Mathf.InverseLerp(volMulBurstThreshold, 3.0f, volMul);    // 1.6~3.0
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
    
    void Awake()
    {
        if (templates == null)
        {
            Debug.Log("[HTSCommunity] templates is null");
            return;
        }

        var seen = new HashSet<int>();
        int dup = 0;
        foreach (var t in templates)
        {
            if (!t) continue;
            if (!seen.Add(t.GetInstanceID())) dup++;
        }

        Debug.Log($"[HTSCommunity] templates={templates.Count}, unique={seen.Count}, dup={dup}");
    }
}