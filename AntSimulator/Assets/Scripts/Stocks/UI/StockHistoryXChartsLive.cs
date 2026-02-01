using System.IO;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class StockHistoryXChartsLive : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public BarChart closeChart;
    public BarChart volumeChart;

    [Header("JSON in StreamingAssets")]
    public string jsonFileName = "stocks_history.json";
    public float pollSeconds = 0.5f;

    string _path;
    System.DateTime _lastWriteUtc;
    float _t;

    void Awake()
    {
        _path = Path.Combine(Application.persistentDataPath, jsonFileName);
        if (!File.Exists(_path))
        {
            _path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        }
        Reload();
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        if (_t < pollSeconds) return;
        _t = 0f;

        if (!File.Exists(_path)) return;

        var wt = File.GetLastWriteTimeUtc(_path);
        if (wt == _lastWriteUtc) return;

        Reload();
    }

    void Reload()
    {
        if (!File.Exists(_path))
        {
            Debug.LogError($"[XChartsLive] JSON not found: {_path}");
            return;
        }

        _lastWriteUtc = File.GetLastWriteTimeUtc(_path);

        var json = File.ReadAllText(_path);
        var data = JsonUtility.FromJson<StockHistory10D>(json);
        if (data == null || data.candles == null || data.candles.Count == 0)
        {
            Debug.LogError("[XChartsLive] parse failed / empty candles");
            return;
        }

        var xLabels = data.candles.Select(c => c.tick.ToString()).ToArray(); // "0"~"9"
        var prices  = data.candles.Select(c => (double)c.price).ToArray();
        var vols    = data.candles.Select(c => (double)c.volume).ToArray();

        ApplyBarChart(closeChart,  $"{data.name} CLOSE",  xLabels, prices);
        ApplyBarChart(volumeChart, $"{data.name} VOLUME", xLabels, vols);
    }

    static void ApplyBarChart(BarChart chart, string title, string[] xLabels, double[] values)
    {
        if (chart == null) return;

        chart.Init();
        chart.EnsureChartComponent<Title>().show = true;
        chart.EnsureChartComponent<Title>().text = title;

        chart.RemoveData();                 // 기존 축/데이터 정리 (예제에서 쓰는 패턴) :contentReference[oaicite:3]{index=3}
        chart.AddSerie<Bar>("S1");

        for (int i = 0; i < xLabels.Length; i++)
        {
            chart.AddXAxisData(xLabels[i]);
            chart.AddData(0, values[i]);    // serie index = 0
        }

        chart.RefreshChart();
    }
}