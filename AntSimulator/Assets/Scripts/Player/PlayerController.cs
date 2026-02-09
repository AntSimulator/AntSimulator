using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
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

        [Header("Popup HTS Tabs")]
        [SerializeField] private bool usePortfolioTab = true;
        [SerializeField] private string tradeTabLabel = "Trading";
        [SerializeField] private string portfolioTabLabel = "Portfolio";

        private PlayerState state;
        private PlayerTradingEngine tradingEngine;
        private SeedPortfolioUseCase seedPortfolioUseCase;
        private int selectedIndex;
        private string pendingSelectedStockId;
        private readonly List<GameObject> tradeTabObjects = new();
        private GameObject portfolioTabPanel;
        private TMP_Text portfolioSummaryText;
        private Button tradeTabButton;
        private Button portfolioTabButton;

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
            TryBuildPortfolioTab();
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

                UpdatePortfolioSummary(null);

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

            UpdatePortfolioSummary(current);
        }

        private void TryBuildPortfolioTab()
        {
            if (!usePortfolioTab || priceText == null || priceText.transform.parent == null)
            {
                return;
            }

            var tradePanelRoot = priceText.transform.parent as RectTransform;
            if (tradePanelRoot == null)
            {
                return;
            }

            tradeTabObjects.Clear();
            for (var index = 0; index < tradePanelRoot.childCount; index++)
            {
                var child = tradePanelRoot.GetChild(index);
                if (child != null)
                {
                    tradeTabObjects.Add(child.gameObject);
                }
            }

            var tabBarObject = new GameObject("TabBar", typeof(RectTransform), typeof(Image));
            var tabBarRect = tabBarObject.GetComponent<RectTransform>();
            tabBarRect.SetParent(tradePanelRoot, false);
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.offsetMin = new Vector2(8f, -34f);
            tabBarRect.offsetMax = new Vector2(-8f, -4f);

            var tabBarImage = tabBarObject.GetComponent<Image>();
            tabBarImage.color = new Color(1f, 1f, 1f, 0.12f);
            tabBarImage.raycastTarget = false;

            tradeTabButton = CreateTabButton("TradeTabButton", tradeTabLabel, tabBarRect, new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            portfolioTabButton = CreateTabButton("PortfolioTabButton", portfolioTabLabel, tabBarRect, new Vector2(0.5f, 0f), new Vector2(1f, 1f));

            tradeTabButton.onClick.AddListener(OpenTradeTab);
            portfolioTabButton.onClick.AddListener(OpenPortfolioTab);

            portfolioTabPanel = new GameObject("PortfolioTabPanel", typeof(RectTransform), typeof(Image));
            var portfolioPanelRect = portfolioTabPanel.GetComponent<RectTransform>();
            portfolioPanelRect.SetParent(tradePanelRoot, false);
            portfolioPanelRect.anchorMin = new Vector2(0f, 0f);
            portfolioPanelRect.anchorMax = new Vector2(1f, 1f);
            portfolioPanelRect.offsetMin = new Vector2(10f, 10f);
            portfolioPanelRect.offsetMax = new Vector2(-10f, -38f);

            var portfolioPanelImage = portfolioTabPanel.GetComponent<Image>();
            portfolioPanelImage.color = new Color(0f, 0f, 0f, 0.18f);

            var summaryObject = new GameObject("SummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var summaryRect = summaryObject.GetComponent<RectTransform>();
            summaryRect.SetParent(portfolioTabPanel.transform, false);
            summaryRect.anchorMin = new Vector2(0f, 0f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.offsetMin = new Vector2(12f, 12f);
            summaryRect.offsetMax = new Vector2(-12f, -12f);

            portfolioSummaryText = summaryObject.GetComponent<TextMeshProUGUI>();
            portfolioSummaryText.alignment = TextAlignmentOptions.TopLeft;
            portfolioSummaryText.fontSize = 20f;
            portfolioSummaryText.enableWordWrapping = false;
            portfolioSummaryText.text = string.Empty;

            OpenTradeTab();
        }

        private Button CreateTabButton(string objectName, string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(parent, false);
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0.5f);

            var button = buttonObject.GetComponent<Button>();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 22f;
            labelText.color = Color.black;
            if (cashText != null && cashText.font != null)
            {
                labelText.font = cashText.font;
            }

            return button;
        }

        private void OpenTradeTab()
        {
            SetPortfolioTabActive(false);
        }

        private void OpenPortfolioTab()
        {
            SetPortfolioTabActive(true);
        }

        private void SetPortfolioTabActive(bool active)
        {
            for (var index = 0; index < tradeTabObjects.Count; index++)
            {
                var child = tradeTabObjects[index];
                if (child != null)
                {
                    child.SetActive(!active);
                }
            }

            if (portfolioTabPanel != null)
            {
                portfolioTabPanel.SetActive(active);
            }

            SetTabButtonVisual(tradeTabButton, !active);
            SetTabButtonVisual(portfolioTabButton, active);
        }

        private static void SetTabButtonVisual(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.45f);
            }
        }

        private void UpdatePortfolioSummary(TestStock current)
        {
            if (!usePortfolioTab || portfolioSummaryText == null || state == null)
            {
                return;
            }

            var summary = new StringBuilder(512);
            summary.AppendLine($"Cash: {state.cash:N0}");

            if (current == null)
            {
                summary.AppendLine("Current selected stock: -");
            }
            else
            {
                var currentQty = state.GetQuantity(current.stockId);
                summary.AppendLine($"Current stock: {current.stockId} / current price {current.currentPrice:N0} / owened {currentQty}");
            }

            summary.AppendLine();
            summary.AppendLine("Portfolio Summary:");

            if (testStocks == null || testStocks.Length == 0)
            {
                summary.AppendLine("- none");
                portfolioSummaryText.text = summary.ToString();
                return;
            }

            for (var index = 0; index < testStocks.Length; index++)
            {
                var stock = testStocks[index];
                if (stock == null || string.IsNullOrWhiteSpace(stock.stockId))
                {
                    continue;
                }

                if (!state.holdings.TryGetValue(stock.stockId, out var holding) || holding == null || holding.quantity <= 0)
                {
                    summary.AppendLine($"- {stock.stockId}: None");
                    continue;
                }

                if (holding.avgBuyPrice <= 0f)
                {
                    summary.AppendLine($"- {stock.stockId}: owned {holding.quantity} stocks / avg buy price unknown");
                    continue;
                }

                var returnRate = (stock.currentPrice - holding.avgBuyPrice) / holding.avgBuyPrice;
                summary.AppendLine($"- {stock.stockId}: owned {holding.quantity} stocks / return {returnRate:P2}");
            }

            portfolioSummaryText.text = summary.ToString();
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
