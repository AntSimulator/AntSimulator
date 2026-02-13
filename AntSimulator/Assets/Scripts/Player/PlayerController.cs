using System.Collections.Generic;
using UnityEngine;
using Player.Core;
using Stocks.Models;
using Stocks.UI;

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

        [Header("Seed Source (Stock SO)")]
        [SerializeField] private bool applySeedOnStart = true;
        [SerializeField] private List<StockDefinition> seedStockDefinitions = new();
        [SerializeField] private MarketSimulator marketSimulator;
        [SerializeField] private long seedBalance = -1;
        [SerializeField] private int seedInitialAmount = 0;
        [SerializeField] private string seedIconColor = "#FFFFFF";
        [SerializeField] private bool seedOverrideStocks = true;

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
        }

        private void Start()
        {
            if (!applySeedOnStart)
            {
                return;
            }

            ApplySeedFromStockDefinitions();
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
                return true;
            }

            return false;
        }

        private void ApplySeedFromStockDefinitions()
        {
            var definitions = ResolveSeedStockDefinitions();
            if (definitions.Count == 0)
            {
                Debug.LogWarning("[Player] No stock definitions available for seed.");
                return;
            }

            var db = StockSeedFactory.BuildFromDefinitions(
                definitions,
                currentBalance: seedBalance,
                defaultAmount: seedInitialAmount,
                defaultIconColor: seedIconColor);

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

        private List<StockDefinition> ResolveSeedStockDefinitions()
        {
            if (seedStockDefinitions != null && seedStockDefinitions.Count > 0)
            {
                return seedStockDefinitions;
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

        public void Buy()
        {
            var current = CurrentStock;
            if (current == null)
            {
                return;
            }

            SyncMarketPrice();
            var result = tradingEngine.Buy(qtyStep);
            if (!result.Success)
            {
                Debug.LogWarning($"[BUY FAIL] {result.Reason}");
            }
            else
            {
                Debug.Log($"[BUY] {current.stockId} x{qtyStep} @ {tradingEngine.CurrentStock.currentPrice}");
            }
        }

        public void Sell()
        {
            var current = CurrentStock;
            if (current == null)
            {
                return;
            }

            SyncMarketPrice();
            var result = tradingEngine.Sell(qtyStep);
            if (!result.Success)
            {
                Debug.LogWarning($"[SELL FAIL] {result.Reason}");
            }
            else
            {
                Debug.Log($"[SELL] {current.stockId} x{qtyStep} @ {tradingEngine.CurrentStock.currentPrice}");
            }
        }

        private void SyncMarketPrice()
        {
            var coreStock = tradingEngine.CurrentStock;
            if (coreStock == null || marketSimulator == null) return;

            var allStocks = marketSimulator.GetAllStocks();
            if (allStocks != null && allStocks.TryGetValue(coreStock.stockId, out var marketState))
            {
                coreStock.SetPrice(Mathf.RoundToInt(marketState.currentPrice));
            }
        }

        public void PriceUp()
        {
            if (!tradingEngine.PriceUp(priceStep))
            {
                return;
            }

            SyncSelectedStockFromEngine();
        }

        public void PriceDown()
        {
            if (!tradingEngine.PriceDown(priceStep))
            {
                return;
            }

            SyncSelectedStockFromEngine();
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

        public int QtyStep => qtyStep;

        public void SetQtyStep(string text)
        {
            if (int.TryParse(text, out var qty) && qty >= 1)
            {
                qtyStep = qty;
            }
        }

        public string SelectedStockId => tradingEngine?.CurrentStock?.stockId;

        public int GetSelectedQuantity() => tradingEngine?.GetCurrentQuantity() ?? 0;

        public float GetSelectedAvgBuyPrice()
        {
            var current = tradingEngine?.CurrentStock;
            if (current == null) return 0f;
            return state.holdings.TryGetValue(current.stockId, out var h) ? h.avgBuyPrice : 0f;
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
