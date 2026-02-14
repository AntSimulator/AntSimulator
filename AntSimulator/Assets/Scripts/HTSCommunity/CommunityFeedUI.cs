using System.Collections.Generic;
using UnityEngine;
using Stocks.UI; // StockSelectionEvents 쓰려면 필요

/// <summary>
/// HTSCommunityManager에서 생성된 HTSPost를 스크롤 리스트로 뿌려주는 UI.
/// - StockSelectionEvents로 종목 선택을 받으면 해당 종목 피드로 리빌드
/// - 새 글이 오면 맨 위에 추가
/// </summary>
public class CommunityFeedUI : MonoBehaviour
{
    [Header("Ref")]
    public HTSCommunityManager htsCommunity;

    [Header("UI")]
    public Transform content;                 // ScrollRect/Viewport/Content
    public HTSPostRowView itemPrefab;         // Item 프리팹 (Text_Title, Text_Content 가진 RowView)

    [Header("Options")]
    [Min(1)] public int maxItems = 80;

    string _currentStockId;
    readonly List<HTSPostRowView> _rows = new();

    void OnEnable()
    {
        // 종목 선택 이벤트 구독
        StockSelectionEvents.OnStockSelected += HandleStockSelected;

        // 커뮤니티 글 생성 이벤트 구독
        if (htsCommunity != null)
            htsCommunity.OnStockPostCreated += HandlePostCreated;
    }

    void OnDisable()
    {
        StockSelectionEvents.OnStockSelected -= HandleStockSelected;

        if (htsCommunity != null)
            htsCommunity.OnStockPostCreated -= HandlePostCreated;
    }

    void HandleStockSelected(string stockCode, string stockName)
    {
        _ = stockName;
        ShowStock(stockCode);
    }

    /// <summary>
    /// 외부에서 직접 호출해도 됨 (StockSelectionEvents 안 쓰면 이거만 호출)
    /// </summary>
    public void ShowStock(string stockId)
    {
        if (string.IsNullOrWhiteSpace(stockId)) return;
        if (_currentStockId == stockId) return;

        _currentStockId = stockId;
        Rebuild();
    }

    void HandlePostCreated(string stockId, HTSPost post)
    {
        // 지금 보고있는 종목만 UI에 반영
        if (string.IsNullOrWhiteSpace(_currentStockId)) return;
        if (!string.Equals(stockId, _currentStockId, System.StringComparison.Ordinal)) return;

        AddRowOnTop(post);
    }

    void Rebuild()
    {
        ClearRows();

        if (htsCommunity == null || content == null || itemPrefab == null) return;
        if (string.IsNullOrWhiteSpace(_currentStockId)) return;

        // 기존 데이터는 "아래로" 쌓기 (옛글 -> 아래 / 최신은 아래쪽)
        // 근데 우리는 UI에서 "최신이 위"를 원하니까:
        // - 과거부터 읽어서 AddRowOnTop 하면 역순이 되니
        // - 과거부터 AddRowOnBottom 하고, 새 글은 AddRowOnTop으로 처리
        var posts = htsCommunity.GetPosts(_currentStockId);
        for (int i = 0; i < posts.Count; i++)
        {
            AddRowOnBottom(posts[i]);
        }
    }

    void AddRowOnTop(HTSPost post)
    {
        if (itemPrefab == null || content == null) return;

        var row = Instantiate(itemPrefab, content);
        row.Bind(post);

        // 맨 위로
        row.transform.SetSiblingIndex(0);

        _rows.Insert(0, row);
        TrimOldFromBottom();
    }

    void AddRowOnBottom(HTSPost post)
    {
        if (itemPrefab == null || content == null) return;

        var row = Instantiate(itemPrefab, content);
        row.Bind(post);

        // 맨 아래(그냥 마지막)
        row.transform.SetSiblingIndex(content.childCount - 1);

        _rows.Add(row);
        TrimOldFromTopIfNeeded(); // 리빌드 때도 max 제한
    }

    void TrimOldFromBottom()
    {
        if (maxItems <= 0) return;

        while (_rows.Count > maxItems)
        {
            var last = _rows[_rows.Count - 1];
            _rows.RemoveAt(_rows.Count - 1);
            if (last) Destroy(last.gameObject);
        }
    }

    void TrimOldFromTopIfNeeded()
    {
        if (maxItems <= 0) return;

        while (_rows.Count > maxItems)
        {
            var first = _rows[0];
            _rows.RemoveAt(0);
            if (first) Destroy(first.gameObject);
        }
    }

    void ClearRows()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i]) Destroy(_rows[i].gameObject);
        }
        _rows.Clear();

        // 혹시 content 밑에 직접 붙어있던 것들도 싹 지우고 싶으면 이거 사용:
        // for (int i = content.childCount - 1; i >= 0; i--)
        //     Destroy(content.GetChild(i).gameObject);
    }
}