using System.IO;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;
using Stocks.Core;
using Stocks.Models;

namespace Stocks.UI
{
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

        [Header("Chart Rect Layout")]
        public bool forceChartRectToFillParent = true;
        [Min(0f)] public float chartInsetLeft = 0f;
        [Min(0f)] public float chartInsetRight = 0f;
        [Min(0f)] public float chartInsetTop = 0f;
        [Min(0f)] public float chartInsetBottom = 0f;


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

        private long _lastSize = -1;

        bool _barAnimated;
        bool _priceAnimated;


        void OnEnable()
        {
            StockSelectionEvents.OnStockSelected += HandleStockSelected;
        }

        void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= HandleStockSelected;
        }

        void Awake()
        {
            var p = Path.Combine(Application.persistentDataPath, jsonFileName);
            _path = File.Exists(p) ? p : null;
            if (priceStick) priceStick.Init();
            if (volumeChart) volumeChart.Init();
            ApplyChartRectLayout();

            Reload();
        }

        void HandleStockSelected(string code, string name)
        {
            _ = name;
            _barAnimated = false;
            _priceAnimated = false;
            ShowStock(code);
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_t < pollSeconds) return;
            _t = 0f;

            if (!File.Exists(_path)) return;

            var p = Path.Combine(Application.persistentDataPath, jsonFileName);
            if (_path != p && File.Exists(p))
            {
                _path = p;
                _lastWriteUtc = default;
            }
            if(!File.Exists(_path)) return;

            var info = new FileInfo(_path);
            var wt = info.LastWriteTimeUtc;
            var size = info.Length;
            if (wt == _lastWriteUtc && size == _lastSize) return;

            _lastWriteUtc = wt;
            _lastSize = size;

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
            ApplyChartRectLayout();
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
                var selected = StockHistorySelector.SelectByCodeOrFirst(_db, _selectedCode);
                if (selected == null)
                {
                    Debug.LogWarning($"[XChartsLive] No history for code={_selectedCode}. Clearing charts.");
                    ClearCharts();
                    return;
                }

                RenderStock(selected);
                return;
            }

            var fallback = StockHistorySelector.SelectByCodeOrFirst(_db, null);
            if (fallback != null)
            {
                RenderStock(fallback);
            }
        }

        void RenderStock(StockHistory10D data)
        {
            if (data == null || data.candles == null || data.candles.Count == 0) return;

            var projection = StockHistoryProjection.Build(data);
            if (projection.XLabels.Length == 0)
            {
                return;
            }

            ApplyCandlestickChart(priceStick, $"{projection.DisplayName} PRICE", projection.XLabels, projection.Ohlc, !_priceAnimated);
            ApplyBarChart(volumeChart, $"{projection.DisplayName} VOLUME", projection.XLabels, projection.Volumes, !_barAnimated);

            _priceAnimated = true;
            _barAnimated = true;

            _lastDataCount = projection.XLabels.Length;
            SetupDataZoom(_lastDataCount);
            _pendingRefresh = true;
        }

        void ClearCharts()
        {
            ClearChart(priceStick);
            ClearChart(volumeChart);
            _barAnimated = false;
            _priceAnimated = false;
            ApplyChartRectLayout();
        }

        static void ClearChart(BaseChart chart)
        {
            if (!chart) return;

            chart.Init();
            chart.RemoveData();
            chart.RefreshChart();
        }

        void ApplyChartRectLayout()
        {
            if (!forceChartRectToFillParent) return;

            ApplyRectLayout(priceStick ? priceStick.rectTransform : null);
            ApplyRectLayout(volumeChart ? volumeChart.rectTransform : null);
        }

        void ApplyRectLayout(RectTransform rt)
        {
            if (!rt) return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(chartInsetLeft, chartInsetBottom);
            rt.offsetMax = new Vector2(-chartInsetRight, -chartInsetTop);
        }

        void SetupDataZoom(int dataCount)
        {
            if (!useDataZoom) return;
            if (!priceStick || !volumeChart) return;

            _priceZoom = ConfigureDataZoom(priceStick, supportSlider: false);
            _volumeZoom = ConfigureDataZoom(volumeChart, supportSlider: true);

            _priceZoom.startEndFunction = (ref float start, ref float end) =>
                SyncZoom(_volumeZoom, volumeChart, start, end);

            _volumeZoom.startEndFunction = (ref float start, ref float end) =>
                SyncZoom(_priceZoom, priceStick, start, end);

            ApplyInitialZoom(dataCount);
        }

        DataZoom ConfigureDataZoom(BaseChart chart, bool supportSlider = true)
        {
            var zoom = chart.EnsureChartComponent<DataZoom>();
            zoom.enable = true;
            zoom.supportInside = true;
            zoom.supportInsideDrag = false;
            zoom.supportInsideScroll = true;
            zoom.supportSlider = supportSlider;
            zoom.supportMarquee = false;
            zoom.zoomLock = false;
            zoom.filterMode = DataZoom.FilterMode.Filter;
            zoom.xAxisIndexs = new List<int> { 0 };
            zoom.yAxisIndexs = new List<int>();
            zoom.rangeMode = DataZoom.RangeMode.Percent;
            zoom.minZoomRatio = Mathf.Clamp(minZoomRatio, 0.01f, 1f);
            zoom.scrollSensitivity = scrollSensitivity;

            if (supportSlider)
            {
                zoom.top = 0.8f;
                zoom.bottom = 5f;
                zoom.left = 75f;
                zoom.right = 20f;
            }

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
                if (hasValidRange)
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


        static void ConfigureTooltip(BaseChart chart)
        {
            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.trigger = Tooltip.Trigger.Axis;
            tooltip.triggerOn = Tooltip.TriggerOn.Click;
            tooltip.position = Tooltip.Position.Auto;
        }

        static void ApplyCandlestickChart(CandlestickChart chart, string title, string[] xLabels, OhlcPoint[] ohlc, bool animate = true)
        {
            if (!chart) return;

            var t = chart.EnsureChartComponent<Title>();
            t.show = false;

            ConfigureTooltip(chart);

            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.left = 75; grid.right = 20; grid.top = 20; grid.bottom = 50;

            chart.RemoveData();
            chart.AddSerie<Candlestick>("S1");
            if (!animate)
                chart.series[0].animation.enable = false;

            var serie = chart.series[0];
            serie.barWidth = 20;
            serie.barGap = 30;
            
            var xaxis = chart.EnsureChartComponent<XAxis>();
            xaxis.type = XAxis.AxisType.Category;

            var yaxis = chart.EnsureChartComponent<YAxis>();
            yaxis.splitNumber = 6;
            yaxis.axisLabel.numericFormatter = "f0";

            for (int i = 0; i < xLabels.Length; i++)
            {
                chart.AddXAxisData(xLabels[i]);
                chart.AddData(0, new[] { ohlc[i].Open, ohlc[i].Close, ohlc[i].Low, ohlc[i].High });
            }

            chart.RefreshChart();
        }

        static void ApplyBarChart(BarChart chart, string title, string[] xLabels, double[] values, bool animate = true)
        {
            if (!chart) return;

            var t = chart.EnsureChartComponent<Title>();
            t.show = false;

            ConfigureTooltip(chart);

            var grid = chart.EnsureChartComponent<GridCoord>();
            grid.left = 75; grid.right = 20; grid.top = 20; grid.bottom = 50;

            chart.RemoveData();
            chart.AddSerie<Bar>("S1");
            if (!animate)
                chart.series[0].animation.enable = false;

            var serie = chart.series[0];
            serie.barWidth = 20;
            serie.barGap = 30;
            
            var xaxis = chart.EnsureChartComponent<XAxis>();
            xaxis.type = XAxis.AxisType.Category;

            var yaxis = chart.EnsureChartComponent<YAxis>();
            yaxis.splitNumber = 3;
            yaxis.axisLabel.numericFormatter = "f0";

            for (int i = 0; i < xLabels.Length; i++)
            {
                chart.AddXAxisData(xLabels[i]);
                chart.AddData(0, values[i]);
            }

            chart.RefreshChart();
        }
    }
}
