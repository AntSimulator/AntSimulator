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
    [Tooltip("하루에 무조건 1개만 올라감")]
    public int infoPostsPerDay = 1;

    [Header("Post Templates (must contain ALL events)")]
    public List<CommunityPostSO> templates = new();

    [Header("Debug")]
    public bool logWhenPosted = true;

    // UI가 읽을 데이터
    public readonly List<CommunityPost> posts = new();
    public event Action<CommunityPost> OnPostCreated;

    private readonly List<ScheduledPost> todaySchedule = new();
    private int todayDay = -1;

    [Serializable]
    private class ScheduledPost
    {
        public int day;
        public int tickInDay;
        public string linkedEventId;
    }

    public void OnDayStarted(int day)
    {
        todayDay = day;
        todaySchedule.Clear();

        if (eventDb == null && eventManager != null)
            eventDb = eventManager.db;

        // 내일 터질 이벤트 하나 선택
        string tomorrowEventId = eventManager != null
            ? eventManager.PickTomorrowForeshadow(day)?.eventId
            : null;

        if (string.IsNullOrEmpty(tomorrowEventId))
        {
            Debug.LogWarning($"[GlobalCommunity] No tomorrow event found for Day{day}");
            return;
        }

        // 하루에 1개만 올라가게 강제
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
                    Debug.Log($"[GlobalCommunity][D{day} T{tickInDay}] {post.title} | event={post.linkedEventId}");

                OnPostCreated?.Invoke(post);
            }

            todaySchedule.RemoveAt(i);
        }
    }

    public IReadOnlyList<CommunityPost> GetPosts() => posts;

    private CommunityPost CreatePostFromSchedule(ScheduledPost s)
    {
        if (string.IsNullOrEmpty(s.linkedEventId))
        {
            Debug.LogError("[GlobalCommunity] linkedEventId is null/empty. This should never happen.");
            return null;
        }

        if (templates == null || templates.Count == 0)
        {
            Debug.LogError("[GlobalCommunity] templates list is empty.");
            return null;
        }

        // eventId로 SO 찾기 (1:1 강제)
        var so = templates.FirstOrDefault(t => t != null && t.eventId == s.linkedEventId);

        if (so == null)
        {
            Debug.LogError($"[GlobalCommunity] Missing CommunityPostSO for eventId={s.linkedEventId}. 반드시 있어야 함.");
            return null;
        }

        return new CommunityPost
        {
            id = Guid.NewGuid().ToString("N"),
            day = s.day,
            tickInDay = s.tickInDay,
            title = so.title,
            body = so.body,
            linkedEventId = s.linkedEventId
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