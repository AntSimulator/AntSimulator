using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GlobalCommunityManager : MonoBehaviour
{
    [Header("Refs")]
    public EventManager eventManager;         
    public EventDatabaseSO eventDb;           // eventId -> def 조회

    [Header("Timing")]
    public int ticksPerDay = 300;             // 하루 틱 수
    public int morningTick = 0;               

    [Header("Daily Post Counts (Global)")]
    public int infoPostsPerDay = 3;           // 정보글 3개
    public int memePostsPerDay = 2;
    public int bragPostsPerDay = 1;
    public int lossPostsPerDay = 1;

    [Header("Templates")]
    public List<CommunityPostSO> templates = new();

    [Header("Debug")]
    public bool logWhenPosted = true; 
    
    // 외부(UI)가 읽을 데이터
    public readonly List<CommunityPost> posts = new();

    public event Action<CommunityPost> OnPostCreated;

    // 오늘 스케줄(틱 단위로 언제 글이 생성될지)
    private readonly List<ScheduledPost> todaySchedule = new();
    private int todayDay = -1;
    
    [Serializable]
    private class ScheduledPost
    {
        public int day;
        public int tickInDay;
        public CommunityPostType type;

        public string linkedEventId;
        public string relatedStockId;
    }

    public void OnDayStarted(int day)
    {
        todayDay = day;
        todaySchedule.Clear();
        if (eventDb == null && eventManager != null) eventDb = eventManager.db;
        AddScheduled(new ScheduledPost
        {
            day = day,
            tickInDay = morningTick,
            type = CommunityPostType.EventReaction
        });

        int foreshadowIdx = UnityEngine.Random.Range(0, Mathf.Max(1, infoPostsPerDay));
        string tomorrowEventId = eventManager != null ? eventManager.PickTomorrowForeshadow(day)?.eventId : null;

        for (int i = 0; i < infoPostsPerDay; i++)
        {
            var sp = new ScheduledPost
            {
                day = day,
                tickInDay = RandomTickAvoidMorning(),
                type = CommunityPostType.Info
            };
            if (i == foreshadowIdx && !string.IsNullOrEmpty(tomorrowEventId))
            {
                sp.linkedEventId = tomorrowEventId;
            }

            AddScheduled(sp);
        }

        for (int i = 0; i < memePostsPerDay; i++)
        {
            AddScheduled(new ScheduledPost
                { day = day, tickInDay = RandomTickAvoidMorning(), type = CommunityPostType.Meme });
        }
        for (int i = 0; i < bragPostsPerDay; i++)
        {
            AddScheduled(new ScheduledPost
                { day = day, tickInDay = RandomTickAvoidMorning(), type = CommunityPostType.Brag });
        }
        for (int i = 0; i < lossPostsPerDay; i++)
        {
            AddScheduled(new ScheduledPost
                { day = day, tickInDay = RandomTickAvoidMorning(), type = CommunityPostType.Loss });
        }
        
        todaySchedule.Sort((a, b) => a.tickInDay.CompareTo(b.tickInDay));
        Debug.Log($"[GlobalCommunity] Day{day} schedule created: {todaySchedule.Count} posts");

    }

    public void OnTick(int day, int tickInDay)
    {
        if (todayDay != day)
        {
            OnDayStarted(day);
        }

        for (int i = todaySchedule.Count - 1; i >= 0; i--)
        {
            var s = todaySchedule[i];
            if(s.day != day) continue;
            if(s.tickInDay != tickInDay) continue;

            var post = CreatePostFromSchedule(s);
            if (post != null)
            {
                posts.Add(post);
                if (logWhenPosted)
                {
                    Debug.Log($"[GlobalCommunity][D{day} T{tickInDay}] {post.type} | {post.title} | event={post.linkedEventId}");
                }
                OnPostCreated?.Invoke(post);
            }
            todaySchedule.RemoveAt(i);
        }
    }

    public IReadOnlyList<CommunityPost> GetPosts() => posts;

    private CommunityPost CreatePostFromSchedule(ScheduledPost s)
    {
        var template = PickTemplateByType(s.type);
        if (template == null) return null;
        return new CommunityPost
        {
            id = Guid.NewGuid().ToString("N"),
            day = s.day,
            tickInDay = s.tickInDay,
            type = template.type,
            title = template.title,
            body = template.body,
            linkedEventId = template.allowAttachEventId ? s.linkedEventId : null,
            relatedStockId = template.allowAttachStockId ? s.relatedStockId : null,
            templateId = template.templateId
        };
        
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
