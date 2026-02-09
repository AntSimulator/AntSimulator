using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Player.Core;
using Stocks.Models;
using Stocks.UI;
using Utils.UnityAdapter;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        [System.Serializable]
        public class TestStock
        {
            public string stockId = "ANT_CO";
            public int currentPrice = 1000;
        }

        [Header("Test Stocks")]
        [SerializeField] private TestStock[] testStocks;

        [Header("Trade Settings")]
        [SerializeField] private int qtyStep = 1;
        [SerializeField] private int priceStep = 500;
        [SerializeField] private long startCash = 100000;
        [SerializeField] private int seedDefaultPrice = 1000;

        [Header("Seed JSON")]
        [SerializeField] private bool applySeedOnStart = true;
        [SerializeField] private string seedJsonFileName = "market_seed.json";
        [SerializeField] private bool seedOverrideStocks = true;

        [Header("UI (TMP)")]
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text qtyText;

        private PlayerState state;
        private PlayerTradingEngine tradingEngine;
        private SeedPortfolioUseCase seedPortfolioUseCase;
        private int selectedIndex;
        private string pendingSelectedStockId;

        private void OnEnable()
        {
            StockSelectionEvents.OnStockSelected += OnStockSelectedFromRow;
        }

        private void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= OnStockSelectedFromRow;
        }

        private TestStock CurrentStock
        {
            get
            {
                if (testStocks == null || testStocks.Length == 0)
                {
                    return null;
                }

                selectedIndex = Mathf.Clamp(selectedIndex, 0, testStocks.Length - 1);
                return testStocks[selectedIndex];
            }
        }

        private void Awake()
        {
            state = new PlayerState(startCash);
            tradingEngine = new PlayerTradingEngine(state);
            seedPortfolioUseCase = new SeedPortfolioUseCase();

            EnsureDefaultStocks();
            tradingEngine.ReplaceStocks(ToCoreStocks(testStocks));

            tradingEngine.SelectByIndex(selectedIndex);
            UpdateUI();
        }

        private async void Start()
        {
            if (!applySeedOnStart)
            {
                return;
            }

            await ApplySeedAsync();
        }

        private void EnsureDefaultStocks()
        {
            if (testStocks != null && testStocks.Length > 0)
            {
                return;
            }

            testStocks = new[]
            {
                new TestStock
                {
                    stockId = "ANT_CO",
                    currentPrice = 1000
                }
            };
        }

        public bool SelectStockById(string stockId)
        {
            if (string.IsNullOrWhiteSpace(stockId) || testStocks == null || testStocks.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < testStocks.Length; i++)
            {
                if (!string.Equals(testStocks[i].stockId, stockId, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                selectedIndex = i;
                tradingEngine.SelectByIndex(i);
                pendingSelectedStockId = null;

                UpdateUI();
                return true;
            }

            return false;
        }

        private async Task ApplySeedAsync()
        {
            var db = await LoadSeedAsync(seedJsonFileName);
            if (db == null)
            {
                return;
            }

            var result = seedPortfolioUseCase.Apply(
                state,
                ToCoreStocks(testStocks),
                db,
                seedOverrideStocks,
                seedDefaultPrice);

            testStocks = ToTestStocks(result.Stocks);
            EnsureDefaultStocks();

            selectedIndex = seedOverrideStocks
                ? 0
                : Mathf.Clamp(selectedIndex, 0, testStocks.Length - 1);

            tradingEngine.ReplaceStocks(ToCoreStocks(testStocks));
            if (!string.IsNullOrWhiteSpace(pendingSelectedStockId) && SelectStockById(pendingSelectedStockId))
            {
                pendingSelectedStockId = null;
            }
            else
            {
                tradingEngine.SelectByIndex(selectedIndex);
            }

            Debug.Log($"[Player] Seed applied cash={state.cash} stocks={testStocks.Length}");
            UpdateUI();
        }

        private void OnStockSelectedFromRow(string stockCode, string stockName)
        {
            _ = stockName;

            if (!SelectStockById(stockCode))
            {
                pendingSelectedStockId = stockCode;
                Debug.LogWarning($"[Player] No matching trade stock for code={stockCode}. Pending selection.");
            }
        }

        private async Task<StockSeedDatabase> LoadSeedAsync(string fileName)
        {
            var load = await StreamingAssetsJsonLoader.LoadTextAsync(fileName);
            if (!load.Success)
            {
                Debug.LogWarning($"[Player] Seed load failed: {load.Error} ({load.Path})");
                return null;
            }

            return JsonUtility.FromJson<StockSeedDatabase>(load.Text);
        }

        public void Buy()
        {
            var current = CurrentStock;
            if (current == null)
            {
                return;
            }

            var result = tradingEngine.Buy(qtyStep);
            if (!result.Success)
            {
                Debug.LogWarning($"[BUY FAIL] {result.Reason}");
            }
            else
            {
                Debug.Log($"[BUY] {current.stockId} x{qtyStep} @ {current.currentPrice}");
            }

            UpdateUI();
        }

        public void Sell()
        {
            var current = CurrentStock;
            if (current == null)
            {
                return;
            }

            var result = tradingEngine.Sell(qtyStep);
            if (!result.Success)
            {
                Debug.LogWarning($"[SELL FAIL] {result.Reason}");
            }
            else
            {
                Debug.Log($"[SELL] {current.stockId} x{qtyStep} @ {current.currentPrice}");
            }

            UpdateUI();
        }

        public void PriceUp()
        {
            if (!tradingEngine.PriceUp(priceStep))
            {
                return;
            }

            SyncSelectedStockFromEngine();
            UpdateUI();
        }

        public void PriceDown()
        {
            if (!tradingEngine.PriceDown(priceStep))
            {
                return;
            }

            SyncSelectedStockFromEngine();
            UpdateUI();
        }

        private void SyncSelectedStockFromEngine()
        {
            if (testStocks == null || testStocks.Length == 0)
            {
                return;
            }

            var coreStock = tradingEngine.CurrentStock;
            if (coreStock == null)
            {
                return;
            }

            var local = CurrentStock;
            if (local == null)
            {
                return;
            }

            local.currentPrice = coreStock.currentPrice;
        }

        private void UpdateUI()
        {
            var current = CurrentStock;

            if (cashText != null)
            {
                cashText.text = $"Cash: {state.cash}";
            }

            if (current == null)
            {
                if (priceText != null)
                {
                    priceText.text = "Price: -";
                }

                if (qtyText != null)
                {
                    qtyText.text = "Qty: -";
                }

                return;
            }

            if (priceText != null)
            {
                priceText.text = $"Price: {current.currentPrice}";
            }

            if (qtyText != null)
            {
                qtyText.text = $"Qty: {state.GetQuantity(current.stockId)}";
            }
        }

        public long Cash
        {
            get => state != null ? state.cash : 0;
            set
            {
                if (state == null)
                {
                    return;
                }

                state.SetCash(value);
                UpdateUI();
            }
        }

        private static List<TradeStockState> ToCoreStocks(IReadOnlyList<TestStock> source)
        {
            var result = new List<TradeStockState>();
            if (source == null)
            {
                return result;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var stock = source[i];
                if (stock == null || string.IsNullOrWhiteSpace(stock.stockId))
                {
                    continue;
                }

                result.Add(new TradeStockState(stock.stockId, stock.currentPrice));
            }

            return result;
        }

        private static TestStock[] ToTestStocks(IReadOnlyList<TradeStockState> source)
        {
            if (source == null || source.Count == 0)
            {
                return new TestStock[0];
            }

            var result = new TestStock[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var stock = source[i];
                result[i] = new TestStock
                {
                    stockId = stock?.stockId ?? string.Empty,
                    currentPrice = stock?.currentPrice ?? 1
                };
            }

            return result;
        }
    }
}
