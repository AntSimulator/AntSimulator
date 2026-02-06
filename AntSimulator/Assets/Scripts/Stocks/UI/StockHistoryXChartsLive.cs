using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class StockHistoryXChartsLive : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public CandlestickChart priceStick;
    public BarChart volumeChart;

    [Header("DataZoom (inside pan + zoom)")]
    public bool useDataZoom = true;
    [Min(5)] public int initialVisibleCount = 60;
    [Range(0.01f, 1f)] public float minZoomRatio = 0.1f;
    [Range(1f, 20f)] public float scrollSensitivity = 4f;
    public bool keepEndOnUpdate = true;


    [Header("JSON in StreamingAssets")]
    public string jsonFileName = "stocks_history.json";
    public float pollSeconds = 0.5f;

    string _path;
    System.DateTime _lastWriteUtc;
    float _t;
    StockHistoryDatabase _db;
    string _selectedCode;
    int _lastDataCount;
    bool _pendingRefresh;

    DataZoom _priceZoom;
    DataZoom _volumeZoom;
    bool _isSyncingZoom;


    void Awake()
    {
        _path = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        if (priceStick) priceStick.Init();
        if (volumeChart) volumeChart.Init();

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

    void LateUpdate()
    {
        if (!_pendingRefresh) return;
        _pendingRefresh = false;

        if (useDataZoom && _lastDataCount > 0)
        {
            ApplyInitialZoom(_lastDataCount);
        }

        if (priceStick) priceStick.RefreshChart();
        if (volumeChart) volumeChart.RefreshChart();
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
        var db = JsonUtility.FromJson<StockHistoryDatabase>(json);

        if (db == null || db.stocks == null || db.stocks.Count == 0)
        {
            var single = JsonUtility.FromJson<StockHistory10D>(json);
            if (single == null || single.candles == null || single.candles.Count == 0)
            {
                Debug.LogError("[XChartsLive] parse failed / empty candles");
                return;
            }

            db = new StockHistoryDatabase { stocks = new List<StockHistory10D> { single } };
        }

        _db = db;
        RenderSelectedOrFirst();
    }

    public void ShowStock(string code, string name = null)
    {
        _ = name;
        _selectedCode = code;

        if (_db == null)
        {
            Reload();
            return;
        }

        RenderSelectedOrFirst();
    }

    void RenderSelectedOrFirst()
    {
        if (_db == null || _db.stocks == null || _db.stocks.Count == 0) return;

        if (!string.IsNullOrWhiteSpace(_selectedCode))
        {
            var data = _db.stocks.FirstOrDefault(s => s.code == _selectedCode);
            if (data != null)
            {
                RenderStock(data);
            }
            else
            {
                Debug.LogWarning($"[XChartsLive] No history for code={_selectedCode}. Clearing charts.");
                ClearCharts();
            }
            return;
        }

        RenderStock(_db.stocks[0]);
    }

    void RenderStock(StockHistory10D data)
    {
        if (data == null || data.candles == null || data.candles.Count == 0) return;

        var xLabels = data.candles.Select(c => c.tick.ToString()).ToArray();
        var prices  = data.candles.Select(c => (double)c.price).ToArray();
        var vols    = data.candles.Select(c => (double)c.volume).ToArray();

        ApplyCandlestickChart(priceStick, $"{data.name} PRICE", xLabels, prices); // ✅ 캔들
        ApplyBarChart(volumeChart, $"{data.name} VOLUME", xLabels, vols);        // ✅ 바

        _lastDataCount = xLabels.Length;
        SetupDataZoom(_lastDataCount);
        _pendingRefresh = true;
    }

    void ClearCharts()
    {
        ClearChart(priceStick);
        ClearChart(volumeChart);
    }

    static void ClearChart(BaseChart chart)
    {
        if (!chart) return;

        chart.Init();
        chart.RemoveData();
        chart.RefreshChart();
    }

    void SetupDataZoom(int dataCount)
    {
        if (!useDataZoom) return;
        if (!priceStick || !volumeChart) return;

        _priceZoom = ConfigureDataZoom(priceStick);
        _volumeZoom = ConfigureDataZoom(volumeChart);

        _priceZoom.startEndFunction = (ref float start, ref float end) =>
            SyncZoom(_volumeZoom, volumeChart, start, end);

        _volumeZoom.startEndFunction = (ref float start, ref float end) =>
            SyncZoom(_priceZoom, priceStick, start, end);

        ApplyInitialZoom(dataCount);
    }

    DataZoom ConfigureDataZoom(BaseChart chart)
    {
        var zoom = chart.EnsureChartComponent<DataZoom>();
        zoom.enable = true;
        zoom.supportInside = true;
        zoom.supportInsideDrag = true;
        zoom.supportInsideScroll = true;
        zoom.supportSlider = false;
        zoom.supportMarquee = false;
        zoom.zoomLock = false;
        zoom.filterMode = DataZoom.FilterMode.Filter;
        zoom.xAxisIndexs = new List<int> { 0 };
        zoom.yAxisIndexs = new List<int>();
        zoom.rangeMode = DataZoom.RangeMode.Percent;
        zoom.minZoomRatio = Mathf.Clamp(minZoomRatio, 0.01f, 1f);
        zoom.scrollSensitivity = scrollSensitivity;
        return zoom;
    }

    void ApplyInitialZoom(int dataCount)
    {
        if (dataCount <= 0 || _priceZoom == null || _volumeZoom == null) return;

        int visible = Mathf.Clamp(initialVisibleCount, 1, dataCount);
        float end = 100f;
        float start = 100f * (1f - (float)visible / dataCount);

        if (keepEndOnUpdate)
        {
            bool hasValidRange = _priceZoom.end > _priceZoom.start + 0.01f;
            bool atEnd = hasValidRange && Mathf.Abs(_priceZoom.end - 100f) < 0.001f;
            if (hasValidRange && !atEnd)
            {
                start = _priceZoom.start;
                end = _priceZoom.end;
            }
        }

        SetZoomRange(_priceZoom, priceStick, start, end, force: true);
        SetZoomRange(_volumeZoom, volumeChart, start, end, force: true);
    }

    void SyncZoom(DataZoom targetZoom, BaseChart targetChart, float start, float end)
    {
        if (_isSyncingZoom) return;
        _isSyncingZoom = true;

        SetZoomRange(targetZoom, targetChart, start, end);

        _isSyncingZoom = false;
    }

    void SetZoomRange(DataZoom zoom, BaseChart chart, float start, float end, bool force = false)
    {
        if (zoom == null || chart == null) return;

        start = Mathf.Clamp(start, 0f, 100f);
        end = Mathf.Clamp(end, 0f, 100f);
        if (end < start) end = start;

        if (!force &&
            Mathf.Abs(zoom.start - start) < 0.001f &&
            Mathf.Abs(zoom.end - end) < 0.001f)
            return;

        zoom.start = start;
        zoom.end = end;

        chart.OnDataZoomRangeChanged(zoom);
        chart.RefreshChart();
    }


    static void ApplyCandlestickChart(CandlestickChart chart, string title, string[] xLabels, double[] closes)
    {
        if (!chart) return;

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
