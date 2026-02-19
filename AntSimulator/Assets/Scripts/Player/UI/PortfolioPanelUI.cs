using TMPro;
using UnityEngine;
using Player.Runtime;
using Stocks.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Player.UI
{
    [DisallowMultipleComponent]
    public class PortfolioPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MarketSimulator marketSimulator;

        [Header("Portfolio Panel")]
        [SerializeField] private Transform portfolioContent;
        [SerializeField] private GameObject ownedStockRectPrefab;
        [SerializeField] private bool clearExistingChildrenOnInit = true;

        private const string OwnedStockRectAssetPath = "Assets/Prefab/OwnedStockRect.prefab";

        private const string StockNameNode = "StockName";
        private const string CurrentPriceNode = "CurrentPrice";
        private const string TotalBuyQuantityNode = "TotalBuyQuantity";
        private const string AverageBuyPriceNode = "AverageBuyPrice";
        private const string ProfitRateNode = "ProfitRate";

        private string selectedStockCode;
        private string selectedStockName;

        private GameObject ownedStockRectInstance;
        private TMP_Text stockNameText;
        private TMP_Text currentPriceText;
        private TMP_Text totalBuyQuantityText;
        private TMP_Text averageBuyPriceText;
        private TMP_Text profitRateText;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            StockSelectionEvents.OnStockSelected += HandleStockSelected;

            if (string.IsNullOrWhiteSpace(selectedStockCode) && playerController != null)
            {
                selectedStockCode = playerController.SelectedStockId;
                selectedStockName = ResolveStockDisplayName(selectedStockCode);
            }

            RefreshSelectedStockView();
            RegisterStatusBindings();
        }

        private void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= HandleStockSelected;
            UnregisterStatusBindings();
        }

        private void Update()
        {
            if (string.IsNullOrWhiteSpace(selectedStockCode) &&
                string.IsNullOrWhiteSpace(playerController != null ? playerController.SelectedStockId : null))
            {
                return;
            }

            UpdateOwnedStockRect();
        }

        public void RefreshSelectedStockView()
        {
            EnsureOwnedStockRectInstance();
            UpdateOwnedStockRect();
        }

        private void HandleStockSelected(string stockCode, string stockName)
        {
            selectedStockCode = stockCode;
            selectedStockName = stockName ?? string.Empty;
            RefreshSelectedStockView();
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }

            if (marketSimulator == null)
            {
                marketSimulator = FindObjectOfType<MarketSimulator>();
            }

            if (portfolioContent == null)
            {
                portfolioContent = transform;
            }
        }

        private void EnsureOwnedStockRectInstance()
        {
            if (ownedStockRectInstance != null)
            {
                return;
            }

            ResolveReferences();
            TryResolveOwnedStockRectPrefab();

            if (portfolioContent == null || ownedStockRectPrefab == null)
            {
                return;
            }

            if (clearExistingChildrenOnInit)
            {
                for (var i = portfolioContent.childCount - 1; i >= 0; i--)
                {
                    Destroy(portfolioContent.GetChild(i).gameObject);
                }
            }

            ownedStockRectInstance = Instantiate(ownedStockRectPrefab, portfolioContent);
            CacheOwnedStockTexts(ownedStockRectInstance.transform);
        }

        private void TryResolveOwnedStockRectPrefab()
        {
            if (ownedStockRectPrefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            ownedStockRectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OwnedStockRectAssetPath);
#endif
            if (ownedStockRectPrefab == null)
            {
                Debug.LogWarning("[PortfolioPanelUI] OwnedStockRect prefab is not assigned.");
            }
        }

        private void CacheOwnedStockTexts(Transform root)
        {
            stockNameText = FindText(root, StockNameNode);
            currentPriceText = FindText(root, CurrentPriceNode);
            totalBuyQuantityText = FindText(root, TotalBuyQuantityNode);
            averageBuyPriceText = FindText(root, AverageBuyPriceNode);
            profitRateText = FindText(root, ProfitRateNode);
        }

        private static TMP_Text FindText(Transform root, string nodeName)
        {
            if (root == null || string.IsNullOrWhiteSpace(nodeName))
            {
                return null;
            }

            var target = root.Find(nodeName);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }

        private void UpdateOwnedStockRect()
        {
            if (ownedStockRectInstance == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedStockCode))
            {
                return;
            }

            var runtimeStockId = ResolveRuntimeStockId();
            var displayName = !string.IsNullOrWhiteSpace(selectedStockName)
                ? selectedStockName
                : ResolveStockDisplayName(runtimeStockId);

            var currentPrice = GetMarketPrice(runtimeStockId);

            if (stockNameText != null)
            {
                stockNameText.text = $"Stock: {displayName}";
            }

            if (currentPriceText != null)
            {
                currentPriceText.text = $"Current: {currentPrice:N0}";
            }
        }

        private string ResolveRuntimeStockId()
        {
            var playerSelectedId = playerController != null ? playerController.SelectedStockId : null;
            if (HasMarketStock(playerSelectedId))
            {
                return playerSelectedId;
            }

            if (HasMarketStock(selectedStockCode))
            {
                return selectedStockCode;
            }

            var resolvedByName = ResolveStockIdByDisplayName(selectedStockName);
            if (HasMarketStock(resolvedByName))
            {
                return resolvedByName;
            }

            return selectedStockCode;
        }

        private bool HasMarketStock(string stockId)
        {
            if (string.IsNullOrWhiteSpace(stockId) || marketSimulator == null)
            {
                return false;
            }

            var stocks = marketSimulator.GetAllStocks();
            return stocks != null && stocks.ContainsKey(stockId);
        }

        private float GetMarketPrice(string stockCode)
        {
            if (string.IsNullOrWhiteSpace(stockCode) || marketSimulator == null)
            {
                return 0f;
            }

            var stocks = marketSimulator.GetAllStocks();
            if (stocks != null && stocks.TryGetValue(stockCode, out var stockState))
            {
                return stockState.currentPrice;
            }

            return 0f;
        }

        private string ResolveStockDisplayName(string stockCode)
        {
            if (string.IsNullOrWhiteSpace(stockCode) || marketSimulator == null || marketSimulator.stockDefinitions == null)
            {
                return stockCode ?? string.Empty;
            }

            for (var i = 0; i < marketSimulator.stockDefinitions.Count; i++)
            {
                var def = marketSimulator.stockDefinitions[i];
                if (def == null || !string.Equals(def.stockId, stockCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return string.IsNullOrWhiteSpace(def.displayName) ? stockCode : def.displayName;
            }

            return stockCode;
        }

        private string ResolveStockIdByDisplayName(string stockName)
        {
            if (string.IsNullOrWhiteSpace(stockName) || marketSimulator == null || marketSimulator.stockDefinitions == null)
            {
                return null;
            }

            for (var i = 0; i < marketSimulator.stockDefinitions.Count; i++)
            {
                var def = marketSimulator.stockDefinitions[i];
                if (def == null)
                {
                    continue;
                }

                if (string.Equals(def.displayName, stockName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(def.name, stockName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return def.stockId;
                }
            }

            return null;
        }

        private void RegisterStatusBindings()
        {
            if (PlayerStatusUI.Instance == null) return;

            if (totalBuyQuantityText != null)
                PlayerStatusUI.Instance.RegisterBinding(StatusType.SelectedQuantity, totalBuyQuantityText, "Holding: {0}");

            if (averageBuyPriceText != null)
                PlayerStatusUI.Instance.RegisterBinding(StatusType.SelectedAvgBuyPrice, averageBuyPriceText, "Avg Buy: {0:N2}");

            if (profitRateText != null)
                PlayerStatusUI.Instance.RegisterBinding(StatusType.SelectedProfitRate, profitRateText, "Return: {0:+0.00;-0.00;0.00}%");
        }

        private void UnregisterStatusBindings()
        {
            if (PlayerStatusUI.Instance == null) return;

            if (totalBuyQuantityText != null)
                PlayerStatusUI.Instance.UnregisterBinding(StatusType.SelectedQuantity, totalBuyQuantityText);

            if (averageBuyPriceText != null)
                PlayerStatusUI.Instance.UnregisterBinding(StatusType.SelectedAvgBuyPrice, averageBuyPriceText);

            if (profitRateText != null)
                PlayerStatusUI.Instance.UnregisterBinding(StatusType.SelectedProfitRate, profitRateText);
        }
    }
}
