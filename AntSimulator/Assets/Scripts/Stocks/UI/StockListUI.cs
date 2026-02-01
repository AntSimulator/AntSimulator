using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Stocks.Models;

namespace Stocks.UI
{
    public class StockListUI : MonoBehaviour
    {
    [Header("ScrollRect Content")]
    [SerializeField] private Transform content;        // ScrollRect/Viewport/Content
    [SerializeField] private StockRowView rowPrefab;   // StockRow.prefab

    [Header("Seed JSON")]
    [SerializeField] private string jsonFileName = "stocks_seed.json";

    async void Start()
    {
        Debug.Log("[StockListUI] Start()");

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
        Debug.Log($"[StockListUI] Render() called. count={db.stocks.Count}");

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
            row.Bind(s.name, color);
        }
        
        Debug.Log($"[StockListUI] after render: contentChildCount={content.childCount}");
    }

    static bool TryParseHtmlColor(string html, out Color color)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            color = default;
            return false;
        }
        return ColorUtility.TryParseHtmlString(html, out color);
    }

    async Task<StockSeedDatabase> LoadSeedAsync(string fileName)
    {
        var path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path))
        {
            path = Path.Combine(Application.streamingAssetsPath, fileName);
        }

        // PC/Editor는 File.ReadAllText로 충분
        // Android는 StreamingAssets가 jar 경로라 UnityWebRequest가 안전
        if (path.Contains("://") || path.Contains("jar:"))
        {
            using var req = UnityWebRequest.Get(path);
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[StockListUI] Seed load failed: {req.error} ({path})");
                return null;
            }

            return JsonUtility.FromJson<StockSeedDatabase>(req.downloadHandler.text);
        }
        else
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[StockListUI] Seed file not found: {path}");
                return null;
            }

            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<StockSeedDatabase>(json);
        }
    }
}
}
