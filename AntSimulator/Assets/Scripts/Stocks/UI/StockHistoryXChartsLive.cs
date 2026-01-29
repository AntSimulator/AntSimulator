using System.IO;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class StockHistoryXChartsLive : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public CandlestickChart priceStick;   // ✅ BarChart -> CandlestickChart
    public BarChart volumeChart;

    [Header("JSON in StreamingAssets")]
    public string jsonFileName = "stocks_history.json";
    public float pollSeconds = 0.5f;

    string _path;
    System.DateTime _lastWriteUtc;
    float _t;

    void Awake()
    {
        _path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
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

        var xLabels = data.candles.Select(c => c.tick.ToString()).ToArray();
        var prices  = data.candles.Select(c => (double)c.price).ToArray();
        var vols    = data.candles.Select(c => (double)c.volume).ToArray();

        ApplyCandlestickChart(priceStick, $"{data.name} PRICE", xLabels, prices); // ✅ 캔들
        ApplyBarChart(volumeChart, $"{data.name} VOLUME", xLabels, vols);        // ✅ 바
    }

    static void ApplyCandlestickChart(CandlestickChart chart, string title, string[] xLabels, double[] closes)
    {
        if (!chart) return;

        chart.Init();
        
        var t = chart.EnsureChartComponent<Title>();
        t.show = false;

        var grid = chart.EnsureChartComponent<GridCoord>();
        grid.left = 100; grid.right = 100; grid.top = 30; grid.bottom = 30;

        chart.RemoveData();
        chart.AddSerie<Candlestick>("S1");
        
        var serie = chart.series[0];
        serie.barWidth = 20; 
        serie.barGap = 30;

        double prevClose = closes.Length > 0 ? closes[0] : 0;

        for (int i = 0; i < xLabels.Length; i++)
        {
            // ⚠️ JSON에 OHLC가 없어서 임시로 만든 값들
            double close = closes[i];
            double open  = (i == 0) ? close : prevClose;
            double high  = System.Math.Max(open, close) * 1.01;
            double low   = System.Math.Min(open, close) * 0.99;

            chart.AddXAxisData(xLabels[i]);
            // XCharts 캔들은 보통 [open, close, low, high] 4값을 한 데이터로 넣는 형태야
            chart.AddData(0, new double[] { open, close, low, high });

            prevClose = close;
        }

        chart.RefreshChart();
    }

    static void ApplyBarChart(BarChart chart, string title, string[] xLabels, double[] values)
    {
        if (!chart) return;

        chart.Init();
        
        var t = chart.EnsureChartComponent<Title>();
        t.show = false;

        var grid = chart.EnsureChartComponent<GridCoord>();
        grid.left = 0; grid.right = 0; grid.top = 150; grid.bottom = 100;

        chart.RemoveData();
        chart.AddSerie<Bar>("S1");
        
        var serie = chart.series[0];
        serie.barWidth = 20;
        serie.barGap = 30;

        for (int i = 0; i < xLabels.Length; i++)
        {
            chart.AddXAxisData(xLabels[i]);
            chart.AddData(0, values[i]);
        }

        chart.RefreshChart();
    }
}
