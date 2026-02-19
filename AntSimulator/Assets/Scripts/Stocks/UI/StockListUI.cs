using System.Collections.Generic;
using UnityEngine;
using Stocks.Models;

namespace Stocks.UI
{
    public class StockListUI : MonoBehaviour
    {
        [Header("ScrollRect Content")]
        [SerializeField] private Transform content;        // ScrollRect/Viewport/Content
        [SerializeField] private StockRowView rowPrefab;   // StockRow.prefab

        [Header("Selection")]
        [SerializeField] private bool selectFirstOnRender = true;

        [Header("Source (Stock SO)")]
        [SerializeField] private List<StockDefinition> stockDefinitions = new();
        [SerializeField] private MarketSimulator marketSimulator;

        void Start()
        {
            var db = BuildSeedFromStockDefinitions();

            Debug.Log($"[StockListUI] db null? {db == null}");
            Debug.Log($"[StockListUI] stocks null? {db?.stocks == null}, count={db?.stocks?.Count ?? -1}");
            Debug.Log($"[StockListUI] content={(content ? content.name : "null")}, rowPrefab={(rowPrefab ? rowPrefab.name : "null")}");

            if (db == null || db.stocks == null)
            {
                Debug.LogError("[StockListUI] Seed data is null.");
                return;
            }

            Render(db);
        }

        
        void Render(StockSeedDatabase db)
        {
            if (content == null || rowPrefab == null)
            {
                Debug.LogError("[StockListUI] content 또는 rowPrefab이 비어있음.");
                return;
            }

            // 기존 Row 제거
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            // 생성 + 바인딩
            foreach (var s in db.stocks)
            {
                var row = Instantiate(rowPrefab, content);

                var color = TryParseHtmlColor(s.iconColor, out var c) ? c : Color.white;
                row.Bind(s, color, OnRowSelected);
            }

            if (selectFirstOnRender && db.stocks.Count > 0)
            {
                OnRowSelected(db.stocks[0]);
                
            }
            
        }

        // Row 클릭시 호출
        void OnRowSelected(StockSeedItem item)
        {
            if (item == null) return;

            StockSelectionEvents.RaiseSelected(item.code, item.name);
        }

        // HTML 색상 문자열 파싱 시도
        static bool TryParseHtmlColor(string html, out Color color)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                color = default;
                return false;
            }
            return ColorUtility.TryParseHtmlString(html, out color);
        }

        private StockSeedDatabase BuildSeedFromStockDefinitions()
        {
            var defs = ResolveStockDefinitions();
            if (defs.Count == 0)
            {
                Debug.LogError("[StockListUI] No StockDefinition source found.");
                return null;
            }

            return StockSeedFactory.BuildFromDefinitions(defs);
        }

        private List<StockDefinition> ResolveStockDefinitions()
        {
            if (stockDefinitions != null && stockDefinitions.Count > 0)
            {
                return stockDefinitions;
            }

            if (marketSimulator == null)
            {
                marketSimulator = FindObjectOfType<MarketSimulator>();
            }

            if (marketSimulator != null && marketSimulator.stockDefinitions != null && marketSimulator.stockDefinitions.Count > 0)
            {
                return marketSimulator.stockDefinitions;
            }

            return new List<StockDefinition>();
        }
        
    }
}

