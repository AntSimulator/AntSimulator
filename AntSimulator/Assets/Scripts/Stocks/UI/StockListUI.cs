using System.Threading.Tasks;
using UnityEngine;
using Stocks.Models;
using Utils.UnityAdapter;

namespace Stocks.UI
{
    public class StockListUI : MonoBehaviour
    {
        [Header("ScrollRect Content")]
        [SerializeField] private Transform content;        // ScrollRect/Viewport/Content
        [SerializeField] private StockRowView rowPrefab;   // StockRow.prefab

        [Header("Selection")]
        [SerializeField] private bool selectFirstOnRender = true;

        [Header("Seed JSON")]
        [SerializeField] private string jsonFileName = "market_seed.json";

        //json 불러오기 및 Render 함수 호출
        async void Start()
        {
            var db = await LoadSeedAsync(jsonFileName);

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
        
        

        // Seed JSON 비동기 로드
        async Task<StockSeedDatabase> LoadSeedAsync(string fileName)
        {
            var load = await StreamingAssetsJsonLoader.LoadTextAsync(fileName);
            if (!load.Success)
            {
                Debug.LogError($"[StockListUI] Seed load failed: {load.Error} ({load.Path})");
                return null;
            }

            return JsonUtility.FromJson<StockSeedDatabase>(load.Text);
        }
    }
}

