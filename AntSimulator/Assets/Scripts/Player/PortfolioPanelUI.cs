using TMPro;
using UnityEngine;
using Stocks.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Player
{
    [DisallowMultipleComponent]
    public class PortfolioPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PopupPanelSwitcher popupPanelSwitcher;
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

        private string selectedStockCode;
        private string selectedStockName;

        private GameObject ownedStockRectInstance;
        private TMP_Text stockNameText;
        private TMP_Text currentPriceText;
        private TMP_Text totalBuyQuantityText;
        private TMP_Text averageBuyPriceText;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            StockSelectionEvents.OnStockSelected += HandleStockSelected;

            if (popupPanelSwitcher != null)
            {
                popupPanelSwitcher.PortfolioPanelActiveChanged += HandlePortfolioPanelActiveChanged;
            }

            if (string.IsNullOrWhiteSpace(selectedStockCode) && playerController != null)
            {
                selectedStockCode = playerController.SelectedStockId;
                selectedStockName = ResolveStockDisplayName(selectedStockCode);
            }

            RefreshSelectedStockView();
        }

        private void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= HandleStockSelected;

            if (popupPanelSwitcher != null)
            {
                popupPanelSwitcher.PortfolioPanelActiveChanged -= HandlePortfolioPanelActiveChanged;
            }
        }

        private void Update()
        {
            if (string.IsNullOrWhiteSpace(selectedStockCode))
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

        private void HandlePortfolioPanelActiveChanged(bool isActive)
        {
            if (!isActive)
            {
                return;
            }

            RefreshSelectedStockView();
        }

        private void ResolveReferences()
        {
            if (popupPanelSwitcher == null)
            {
                popupPanelSwitcher = FindObjectOfType<PopupPanelSwitcher>();
            }

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

            var displayName = !string.IsNullOrWhiteSpace(selectedStockName)
                ? selectedStockName
                : ResolveStockDisplayName(selectedStockCode);

            var currentPrice = GetMarketPrice(selectedStockCode);
            var quantity = playerController != null ? playerController.GetSelectedQuantity() : 0;
            var avgBuyPrice = playerController != null ? playerController.GetSelectedAvgBuyPrice() : 0f;

            if (stockNameText != null)
            {
                stockNameText.text = $"Stock: {displayName}";
            }

            if (currentPriceText != null)
            {
                currentPriceText.text = $"Current: {currentPrice:N0}";
            }

            if (totalBuyQuantityText != null)
            {
                totalBuyQuantityText.text = $"Holding: {quantity}";
            }

            if (averageBuyPriceText != null)
            {
                averageBuyPriceText.text = $"Avg Buy: {avgBuyPrice:N2}";
            }
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
    }
}
