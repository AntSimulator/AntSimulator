using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Player.Runtime;
using Stocks.UI;

namespace Player.UI
{
    [DisallowMultipleComponent]
    public class TradePanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MarketSimulator marketSimulator;

        [Header("UI Text")]
        [SerializeField] private TMP_Text stockNameText;
        [SerializeField] private TMP_Text stockBalanceText;
        [SerializeField] private TMP_Text currentPriceText;

        [Header("Input")]
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private Button buyMaxButton;
        [SerializeField] private Button sellMaxButton;

        private string selectedStockCode;
        private string selectedStockName;

        private const string StockNameNode = "StockNameText";
        private const string StockBalanceNode = "StockBalanceText";
        private const string CurrentPriceNode = "CurrentPriceText";
        private const string QuantityInputNode = "InputField (TMP)";
        private const string BuyMaxNode = "BuyMaxButton";
        private const string SellMaxNode = "SellMaxButton";

        private void Awake()
        {
            ResolveReferences();
            ResolveUiReferences();
            SyncSelectedStockFromPlayer();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveUiReferences();

            StockSelectionEvents.OnStockSelected += HandleStockSelected;
            PopupPanelSwitcher.OnPanelChanged += HandlePopupPanelChanged;
            SubscribePlayerEvents();

            if (quantityInput != null)
                quantityInput.onEndEdit.AddListener(OnQuantityInputChanged);

            if (buyMaxButton != null)
                buyMaxButton.onClick.AddListener(OnBuyMaxButtonClicked);

            if (sellMaxButton != null)
                sellMaxButton.onClick.AddListener(OnSellMaxButtonClicked);

            SyncSelectedStockFromPlayer();
            UpdateUI();
        }

        private void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= HandleStockSelected;
            PopupPanelSwitcher.OnPanelChanged -= HandlePopupPanelChanged;
            UnsubscribePlayerEvents();

            if (quantityInput != null)
                quantityInput.onEndEdit.RemoveListener(OnQuantityInputChanged);

            if (buyMaxButton != null)
                buyMaxButton.onClick.RemoveListener(OnBuyMaxButtonClicked);

            if (sellMaxButton != null)
                sellMaxButton.onClick.RemoveListener(OnSellMaxButtonClicked);
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
        }

        private void ResolveUiReferences()
        {
            TryResolveText(ref stockNameText, StockNameNode);
            TryResolveText(ref stockBalanceText, StockBalanceNode);
            TryResolveText(ref currentPriceText, CurrentPriceNode);
            TryResolveInputField(ref quantityInput, QuantityInputNode);
            TryResolveButton(ref buyMaxButton, BuyMaxNode);
            TryResolveButton(ref sellMaxButton, SellMaxNode);
        }

        private void TryResolveText(ref TMP_Text target, string nodeName)
        {
            if (target != null)
            {
                return;
            }

            target = FindInChildrenByName<TMP_Text>(transform, nodeName);
        }

        private void TryResolveInputField(ref TMP_InputField target, string nodeName)
        {
            if (target != null)
            {
                return;
            }

            target = FindInChildrenByName<TMP_InputField>(transform, nodeName);
        }

        private void TryResolveButton(ref Button target, string nodeName)
        {
            if (target != null)
            {
                return;
            }

            target = FindInChildrenByName<Button>(transform, nodeName);
        }

        private static T FindInChildrenByName<T>(Transform root, string nodeName) where T : Component
        {
            if (root == null || string.IsNullOrWhiteSpace(nodeName))
            {
                return null;
            }

            var items = root.GetComponentsInChildren<T>(true);
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] != null && string.Equals(items[i].name, nodeName, System.StringComparison.Ordinal))
                {
                    return items[i];
                }
            }

            return null;
        }

        private void SubscribePlayerEvents()
        {
            if (playerController == null)
            {
                return;
            }

            playerController.OnSelectedStockChanged -= HandlePlayerSelectedStockChanged;
            playerController.OnSelectedStockChanged += HandlePlayerSelectedStockChanged;
            playerController.OnHoldingsChanged -= HandleHoldingsChanged;
            playerController.OnHoldingsChanged += HandleHoldingsChanged;
        }

        private void UnsubscribePlayerEvents()
        {
            if (playerController == null)
            {
                return;
            }

            playerController.OnSelectedStockChanged -= HandlePlayerSelectedStockChanged;
            playerController.OnHoldingsChanged -= HandleHoldingsChanged;
        }

        private void Start()
        {
            if (quantityInput != null && playerController != null)
                quantityInput.text = playerController.QtyStep.ToString();
        }

        private void HandleStockSelected(string stockCode, string stockName)
        {
            var hasChanged = !string.Equals(selectedStockCode, stockCode, System.StringComparison.Ordinal);
            selectedStockCode = stockCode;
            selectedStockName = string.IsNullOrWhiteSpace(stockName)
                ? ResolveStockDisplayName(stockCode)
                : stockName;

            if (hasChanged && quantityInput != null)
            {
                quantityInput.SetTextWithoutNotify("0");
            }

            UpdateUI();
        }

        private void HandlePopupPanelChanged(int _)
        {
            SyncSelectedStockFromPlayer();
            UpdateUI();
        }

        private void HandlePlayerSelectedStockChanged(string stockId)
        {
            if (string.IsNullOrWhiteSpace(stockId))
            {
                return;
            }

            selectedStockCode = stockId;
            selectedStockName = ResolveStockDisplayName(stockId);
            UpdateUI();
        }

        private void HandleHoldingsChanged()
        {
            UpdateUI();
        }

        private void OnQuantityInputChanged(string text)
        {
            if (playerController != null)
                playerController.SetQtyStep(text);
        }

        private void OnBuyMaxButtonClicked()
        {
            if (quantityInput == null || playerController == null)
            {
                return;
            }

            var stockCode = ResolveActiveStockCode();

            if (string.IsNullOrWhiteSpace(stockCode))
            {
                return;
            }

            var currentPrice = GetMarketPrice(stockCode);
            var maxQuantity = CalculateMaxBuyQuantity(playerController.Cash, currentPrice);

            quantityInput.text = maxQuantity.ToString();
            OnQuantityInputChanged(quantityInput.text);
        }

        private void OnSellMaxButtonClicked()
        {
            if (quantityInput == null || playerController == null)
            {
                return;
            }

            var stockCode = ResolveActiveStockCode();
            if (string.IsNullOrWhiteSpace(stockCode))
            {
                return;
            }

            var holdingQuantity = GetHoldingQuantity(stockCode);

            quantityInput.text = holdingQuantity.ToString();
            OnQuantityInputChanged(quantityInput.text);
        }

        private void Update()
        {
            SyncSelectedStockFromPlayer();
            UpdateUI();
        }

        private void UpdateUI()
        {
            var stockCode = ResolveActiveStockCode();
            if (string.IsNullOrWhiteSpace(stockCode))
            {
                return;
            }

            selectedStockCode = stockCode;
            if (string.IsNullOrWhiteSpace(selectedStockName))
            {
                selectedStockName = ResolveStockDisplayName(stockCode);
            }

            float currentPrice = GetMarketPrice(stockCode);
            int holdingQuantity = GetHoldingQuantity(stockCode);

            if (stockNameText != null)
                stockNameText.text = selectedStockName;

            if (stockBalanceText != null)
                stockBalanceText.text = $"보유수량 : {holdingQuantity:N0}주";

            if (currentPriceText != null)
                currentPriceText.text = $"현재가 : {currentPrice:N0}원";
        }

        private string ResolveActiveStockCode()
        {
            if (!string.IsNullOrWhiteSpace(selectedStockCode))
            {
                return selectedStockCode;
            }

            return playerController != null ? playerController.SelectedStockId : null;
        }

        private void SyncSelectedStockFromPlayer()
        {
            if (playerController == null)
            {
                return;
            }

            var playerSelectedId = playerController.SelectedStockId;
            if (string.IsNullOrWhiteSpace(playerSelectedId))
            {
                return;
            }

            if (!string.Equals(selectedStockCode, playerSelectedId, System.StringComparison.OrdinalIgnoreCase))
            {
                selectedStockCode = playerSelectedId;
                selectedStockName = ResolveStockDisplayName(playerSelectedId);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedStockName))
            {
                selectedStockName = ResolveStockDisplayName(playerSelectedId);
            }
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

        private int GetHoldingQuantity(string stockCode)
        {
            if (playerController == null || string.IsNullOrWhiteSpace(stockCode))
            {
                return 0;
            }

            return playerController.GetQuantityByStockId(stockCode);
        }

        private float GetMarketPrice(string stockCode)
        {
            if (marketSimulator == null) return 0f;

            var stocks = marketSimulator.GetAllStocks();
            if (stocks != null && stocks.TryGetValue(stockCode, out var stockState))
            {
                return stockState.currentPrice;
            }

            return 0f;
        }

        private static int CalculateMaxBuyQuantity(long cash, float currentPrice)
        {
            if (cash <= 0 || currentPrice <= 0f)
            {
                return 0;
            }

            var max = System.Math.Floor(cash / (double)currentPrice);
            if (max >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)max;
        }
    }
}
