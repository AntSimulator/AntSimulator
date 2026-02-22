using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using Player.Core;
using Player.Models;
using Stocks.Models;
using Stocks.UI;
using UnityEngine.SceneManagement;

namespace Player.Runtime
{
    public class PlayerController : MonoBehaviour
    {
        private static readonly IReadOnlyDictionary<string, Holding> EmptyHoldings =
            new Dictionary<string, Holding>();

        [System.Serializable]
        public class TestStock
        {
            public string stockId = "ANT_CO";
            public int currentPrice = 1000;
        }

        [Header("Test Stocks")] [SerializeField]
        private TestStock[] testStocks;

        [Header("Trade Settings")] [SerializeField]
        private int qtyStep = 1;

        [SerializeField] private int priceStep = 500;
        [SerializeField] private long startCash = 2000000;
        [SerializeField] private int seedDefaultPrice = 1000;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;

        [Header("HP Settings")] [SerializeField]
        private int startHp = 100;

        [SerializeField] private int maxHp = 100;

        [Header("Seed Source (Stock SO)")] [SerializeField]
        private bool applySeedOnStart = true;

        [SerializeField] private List<StockDefinition> seedStockDefinitions = new();
        [SerializeField] private MarketSimulator marketSimulator;
        [SerializeField] private long seedBalance = -1;
        [SerializeField] private int seedInitialAmount = 0;
        [SerializeField] private string seedIconColor = "#FFFFFF";
        [SerializeField] private bool seedOverrideStocks = true;

        [Header("HP Drain")] [SerializeField] private bool drainHpEnabled = true;
        [SerializeField] private float drainIntervalSeconds = 3f; // 3초마다
        [SerializeField] private int drainAmount = 1; // 1씩 감소
        [SerializeField] private bool drainUseUnscaledTime = false; // TimeScale=0이어도 닳게 할거면 true

        private float hpDrainTimer = 0f;


        private PlayerState state;
        private PlayerHp hp;
        private PlayerTradingEngine tradingEngine;
        private SeedPortfolioUseCase seedPortfolioUseCase;
        private int selectedIndex;
        private string pendingSelectedStockId;

        public event Action<int, int> OnHpChanged;
        public event Action<long> OnCashChanged;
        public event Action OnHoldingsChanged;
        public event Action<string> OnSelectedStockChanged;

        private void OnEnable()
        {
            StockSelectionEvents.OnStockSelected += OnStockSelectedFromRow;
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(Buy);
            }

            if (sellButton != null)
            {
                sellButton.onClick.AddListener(Sell);
            }
        }

        private void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= OnStockSelectedFromRow;
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(Buy);
            }

            if (sellButton != null)
            {
                sellButton.onClick.RemoveListener(Sell);
            }
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

        private void EnsureHpInitialized()
        {
            if (hp != null)
            {
                return;
            }

            hp = new PlayerHp(startHp, maxHp);
            hp.OnHpChanged += HandleHpChanged;
        }

        private void Awake()
        {
            state = new PlayerState(startCash);
            EnsureHpInitialized();
            tradingEngine = new PlayerTradingEngine(state);
            seedPortfolioUseCase = new SeedPortfolioUseCase();

            EnsureDefaultStocks();
            tradingEngine.ReplaceStocks(ToCoreStocks(testStocks));
            tradingEngine.SelectByIndex(selectedIndex);
        }

        private void OnDestroy()
        {
            if (hp != null)
            {
                hp.OnHpChanged -= HandleHpChanged;
            }
        }

        private void Start()
        {
            if (applySeedOnStart)
            {
                ApplySeedFromStockDefinitions();
            }
            else
            {
                OnSelectedStockChanged?.Invoke(SelectedStockId);
            }

            OnCashChanged?.Invoke(Cash);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        private void Update()
        {
            DrainHpTick();
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
                OnSelectedStockChanged?.Invoke(stockId);
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
                OnSelectedStockChanged?.Invoke(SelectedStockId);
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

            if (marketSimulator != null && marketSimulator.stockDefinitions != null &&
                marketSimulator.stockDefinitions.Count > 0)
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
                OnCashChanged?.Invoke(Cash);
                OnHoldingsChanged?.Invoke();
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
                OnCashChanged?.Invoke(Cash);
                OnHoldingsChanged?.Invoke();
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

        public IReadOnlyDictionary<string, Holding> GetHoldings()
        {
            return state != null ? state.holdings : EmptyHoldings;
        }

        public void SetQtyStep(string text)
        {
            if (int.TryParse(text, out var qty) && qty >= 1)
            {
                qtyStep = qty;
            }
        }

        public string SelectedStockId => tradingEngine?.CurrentStock?.stockId;

        public int GetSelectedQuantity() => tradingEngine?.GetCurrentQuantity() ?? 0;

        public int GetQuantityByStockId(string stockId)
        {
            if (state == null || string.IsNullOrWhiteSpace(stockId))
            {
                return 0;
            }

            return state.GetQuantity(stockId);
        }

        public void SetQuantityByStockId(string stockId, int amount)
        {
            if (state == null || string.IsNullOrWhiteSpace(stockId))
            {
                return;
            }

            state.SetHolding(stockId, amount);

            OnHoldingsChanged?.Invoke();
        }

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

                var prev = state.cash;
                state.SetCash(value);
                if (state.cash != prev)
                {
                    OnCashChanged?.Invoke(state.cash);
                }
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

        public long GetCash()
        {
            if (state == null) return 0;
            return state.cash;
        }

        public void SetSaveCash(long amount)
        {
            if (state == null) return;
            state.cash = amount;
        }

        public int CurrentHp
        {
            get { return hp.CurrentHp; }
        }

        public void LoadSavedHp(int savedValue)
        {
            hp.SetHp(savedValue);
        }

        public int MaxHp
        {
            get { return hp.MaxHp; }
        }

        public void AddHp(int amount)
        {

            hp.AddHp(amount);
        }

        public void DecreaseHp(int amount)
        {

            hp.DecreaseHp(amount);
        }

        public void SetHp(int value)
        {

            hp.SetHp(value);
        }

        private void HandleHpChanged(int currentHp, int currentMaxHp)
        {
            OnHpChanged?.Invoke(currentHp, currentMaxHp);
        }

        private void DrainHpTick()
        {
            if (!drainHpEnabled) return;

            EnsureHpInitialized(); // 혹시라도 안전하게

            float dt = drainUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f) return;

            hpDrainTimer += dt;
            if (hpDrainTimer < drainIntervalSeconds) return;

            // 2초 단위로 누적된 만큼 처리 (프레임 드랍 대비)
            while (hpDrainTimer >= drainIntervalSeconds)
            {
                hpDrainTimer -= drainIntervalSeconds;
                hp.DecreaseHp(drainAmount);
            }
        }

        public void AddCash(long amount)
        {
            if (state == null) return;
            state.AddCash(amount);
            OnCashChanged?.Invoke(state.cash);
        }

        public void SubtractCash(long amount)
        {
            if (state == null) return;
            state.RemoveCash(amount);
            OnCashChanged?.Invoke(state.cash);
            if (state.cash < 0)
            {
                TrySellAllHoldings();
            }
        }

        private void TrySellAllHoldings()
        {
            if (state == null) return;

            if (state.holdings.Count == 0)
            {
                SceneManager.LoadScene("BadEndingScene");
            }

            var allMarketStocks = marketSimulator != null ? marketSimulator.GetAllStocks() : null;

            // testStocks 가격 fallback용 딕셔너리
            var testStockPrices = new Dictionary<string, int>();
            if (testStocks != null)
            {
                foreach (var ts in testStocks)
                {
                    if (ts != null && !string.IsNullOrWhiteSpace(ts.stockId))
                        testStockPrices[ts.stockId] = ts.currentPrice;
                }
            }

            // 컬렉션 수정 방지를 위해 복사
            var holdingsCopy = new List<(string stockId, int quantity)>();
            foreach (var kvp in state.holdings)
            {
                if (kvp.Value.quantity > 0)
                    holdingsCopy.Add((kvp.Key, kvp.Value.quantity));
            }

            bool anySold = false;
            foreach (var (stockId, quantity) in holdingsCopy)
            {
                // 시장 가격 우선, 없으면 testStocks 가격, 없으면 1
                int price = 1;
                if (allMarketStocks != null && allMarketStocks.TryGetValue(stockId, out var marketState))
                    price = Mathf.RoundToInt(marketState.currentPrice);
                else if (testStockPrices.TryGetValue(stockId, out var testPrice))
                    price = testPrice;

                if (state.TrySell(stockId, quantity, price, out var reason))
                {
                    Debug.Log($"[Player] 강제 청산: {stockId} x{quantity} @ {price}");
                    anySold = true;
                }
                else
                {
                    Debug.LogWarning($"[Player] 강제 청산 실패: {stockId} - {reason}");
                }
            }

            if (anySold)
            {
                OnCashChanged?.Invoke(state.cash);
                OnHoldingsChanged?.Invoke();
            }

            if (state.cash < 0)
            {
                SceneManager.LoadScene("BadEndingScene");
            }
        }
    }
}
