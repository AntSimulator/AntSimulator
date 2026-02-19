using System.Collections.Generic;
using UnityEngine;

public class GlobalCommunityFeedUI : MonoBehaviour
{
    [Header("Ref")]
    public GlobalCommunityManager globalCommunity;

    [Header("Optional: For header(sector)")]
    public EventDatabaseSO eventDb;   // 이벤트 ID로 섹터/타이틀 뽑으려고

    [Header("UI")]
    public Transform content;
    public GlobalPostRowView itemPrefab;

    [Header("Options")]
    public int maxItems = 80;

    private readonly List<GlobalPostRowView> _rows = new();

    void OnEnable()
    {
        if (globalCommunity != null)
            globalCommunity.OnPostCreated += HandlePostCreated;

        Rebuild();
    }

    void OnDisable()
    {
        if (globalCommunity != null)
            globalCommunity.OnPostCreated -= HandlePostCreated;
    }

    void HandlePostCreated(CommunityPost post)
    {
        AddRowOnTop(post);
    }

    public void Rebuild()
    {
        // 기존 row 제거
        for (int i = 0; i < _rows.Count; i++)
            if (_rows[i]) Destroy(_rows[i].gameObject);
        _rows.Clear();

        if (globalCommunity == null || content == null || itemPrefab == null) return;

        var posts = globalCommunity.GetPosts();
        for (int i = posts.Count - 1; i >= 0; i--)
            AddRowOnBottom(posts[i]);
    }

    void AddRowOnTop(CommunityPost post)
    {
        var row = Instantiate(itemPrefab, content);
        row.Bind(GetHeader(post), post);
        row.transform.SetSiblingIndex(0);

        _rows.Insert(0, row);
        TrimOld();
    }

    void AddRowOnBottom(CommunityPost post)
    {
        var row = Instantiate(itemPrefab, content);
        row.Bind(GetHeader(post), post);
        _rows.Add(row);
        TrimOld();
    }

    void TrimOld()
    {
        if (maxItems <= 0) return;

        while (_rows.Count > maxItems)
        {
            var last = _rows[_rows.Count - 1];
            _rows.RemoveAt(_rows.Count - 1);
            if (last) Destroy(last.gameObject);
        }
    }

    string GetHeader(CommunityPost post)
    {
        // header = “섹터” 표시용
        // 이벤트ID로 이벤트 정의 찾고 sector 표시 (없으면 "GLOBAL" 같은 기본값)
        if (post == null || string.IsNullOrEmpty(post.linkedEventId) || eventDb == null)
            return "GLOBAL";

        var def = eventDb.FindById(post.linkedEventId);
        if (def == null) return "GLOBAL";

        // 네 EventDefinition에 sector 필드 있다고 가정 (없으면 title 같은 걸로 바꿔)
        return string.IsNullOrEmpty(def.sector.ToString()) ? "GLOBAL" : def.sector.ToString();
    }
}