using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalCommunityManager : MonoBehaviour
{
    [Header("Refs")]
    public EventManager eventManager;
    public EventDatabaseSO eventDb;

    [Header("Timing")]
    public int ticksPerDay = 300;
    public int morningTick = 0;

    [Header("Posts")]
    public int infoPostsPerDay = 1;

    [Header("Post Templates (must contain ALL events)")]
    public List<CommunityPostSO> templates = new();

    [Header("Debug")]
    public bool logWhenPosted = true;

    public readonly List<CommunityPost> posts = new();
    public event Action<CommunityPost> OnPostCreated;

    private readonly List<ScheduledPost> todaySchedule = new();
    private int todayDay = -1;

    // ✅ 캐시 맵
    private Dictionary<string, CommunityPostSO> _templateMap;

    [Serializable]
    private class ScheduledPost
    {
        public int day;
        public int tickInDay;
        public string linkedEventId;
    }

    void Awake()
    {
        BuildTemplateMap();
    }

    void OnValidate()
    {
        // 에디터에서 값 바꿀 때도 체크되게
        BuildTemplateMap();
    }

    void BuildTemplateMap()
    {
        _templateMap = new Dictionary<string, CommunityPostSO>(StringComparer.Ordinal);

        if (templates == null) return;

        foreach (var t in templates)
        {
            if (t == null) continue;

            var id = (t.eventId ?? "").Trim();
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[GlobalCommunity] CommunityPostSO '{t.name}' eventId 비어있음. 반드시 eventId 채워야함.");
                continue;
            }

            if (_templateMap.ContainsKey(id))
            {
                Debug.LogError($"[GlobalCommunity] CommunityPostSO eventId 중복: '{id}'. 첫 번째='{_templateMap[id].name}', 중복='{t.name}'");
                continue;
            }

            _templateMap[id] = t;
        }
    }

    public void OnDayStarted(int day)
    {
        todayDay = day;
        todaySchedule.Clear();

        if (eventDb == null && eventManager != null)
            eventDb = eventManager.db;

        // ✅ 캘린더 이벤트 제외한 “내일 이벤트” 고르기
        string tomorrowEventId = PickTomorrowForeshadowNonCalendar(day);

        if (string.IsNullOrEmpty(tomorrowEventId))
        {
            Debug.LogWarning($"[GlobalCommunity] No NON-CALENDAR tomorrow event found for Day{day}");
            return;
        }

        AddScheduled(new ScheduledPost
        {
            day = day,
            tickInDay = RandomTickAvoidMorning(),
            linkedEventId = tomorrowEventId
        });

        todaySchedule.Sort((a, b) => a.tickInDay.CompareTo(b.tickInDay));

        if (logWhenPosted)
            Debug.Log($"[GlobalCommunity] Day{day} schedule created: {todaySchedule.Count} posts (event={tomorrowEventId})");
    }

    // ✅ 핵심: “캘린더 이벤트는 foreshadow 금지”
    private string PickTomorrowForeshadowNonCalendar(int today)
    {
        if (eventManager == null) return null;

        var list = eventManager.GetEventsStartingOnDay(today + 1);
        if (list == null || list.Count == 0) return null;

        // 여기서 def를 찾아서 kind/flag로 캘린더 제외
        var candidates = new List<string>();

        foreach (var inst in list)
        {
            if (inst == null || string.IsNullOrEmpty(inst.eventId)) continue;
            var def = eventDb != null ? eventDb.FindById(inst.eventId) : (eventManager.db != null ? eventManager.db.FindById(inst.eventId) : null);
            if (def == null) continue;

            // ✅ 너 프로젝트에 EventKind.Calendar가 있으면 그걸로
            // 없으면 def.canBeCalendarEvent 같은 플래그 쓰지 말고 “kind”로 분리하는 게 맞음.
            if (def.kind == EventKind.Calendar) continue;

            candidates.Add(def.eventId);
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    public void OnTick(int day, int tickInDay)
    {
        if (todayDay != day)
            OnDayStarted(day);

        for (int i = todaySchedule.Count - 1; i >= 0; i--)
        {
            var s = todaySchedule[i];
            if (s.day != day) continue;
            if (s.tickInDay != tickInDay) continue;

            var post = CreatePostFromSchedule(s);
            if (post != null)
            {
                posts.Add(post);

                if (logWhenPosted)
                    Debug.Log($"[GlobalCommunity][D{day} T{tickInDay}] title='{post.title}' | event={post.linkedEventId}");

                OnPostCreated?.Invoke(post);
            }

            todaySchedule.RemoveAt(i);
        }
    }

    public IReadOnlyList<CommunityPost> GetPosts() => posts;

    private CommunityPost CreatePostFromSchedule(ScheduledPost s)
    {
        var id = (s.linkedEventId ?? "").Trim();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[GlobalCommunity] linkedEventId is null/empty.");
            return null;
        }

        if (_templateMap == null || _templateMap.Count == 0)
        {
            Debug.LogError("[GlobalCommunity] templateMap 비어있음. templates에 eventId 채워졌는지 확인.");
            return null;
        }

        if (!_templateMap.TryGetValue(id, out var so) || so == null)
        {
            Debug.LogError($"[GlobalCommunity] 템플릿 매칭 실패: eventId='{id}'. templates에 같은 eventId의 SO가 있어야 함.");
            return null;
        }

        return new CommunityPost
        {
            id = Guid.NewGuid().ToString("N"),
            day = s.day,
            tickInDay = s.tickInDay,
            title = so.title,
            body = so.title,
            linkedEventId = id
        };
    }

    private void AddScheduled(ScheduledPost s)
    {
        s.tickInDay = Mathf.Clamp(s.tickInDay, 0, Mathf.Max(0, ticksPerDay - 1));
        todaySchedule.Add(s);
    }

    private int RandomTickAvoidMorning()
    {
        int tpd = Mathf.Max(1, ticksPerDay);
        if (tpd <= 1) return 0;

        int t = UnityEngine.Random.Range(0, tpd);
        if (t == morningTick) t = (t + 1) % tpd;
        return t;
    }
}