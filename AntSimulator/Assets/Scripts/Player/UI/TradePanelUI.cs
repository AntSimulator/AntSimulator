using UnityEngine;
using TMPro;
using Player.Runtime;
using Stocks.UI;

namespace Player.UI
{
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

        private string selectedStockCode;
        private string selectedStockName;

        private void OnEnable()
        {
            StockSelectionEvents.OnStockSelected += HandleStockSelected;

            if (quantityInput != null)
                quantityInput.onEndEdit.AddListener(OnQuantityInputChanged);
        }

        private void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= HandleStockSelected;

            if (quantityInput != null)
                quantityInput.onEndEdit.RemoveListener(OnQuantityInputChanged);
        }

        private void Start()
        {
            if (quantityInput != null && playerController != null)
                quantityInput.text = playerController.QtyStep.ToString();
        }

        private void HandleStockSelected(string stockCode, string stockName)
        {
            selectedStockCode = stockCode;
            selectedStockName = stockName;
        }

        private void OnQuantityInputChanged(string text)
        {
            if (playerController != null)
                playerController.SetQtyStep(text);
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(selectedStockCode)) return;

            UpdateUI();
        }

        private void UpdateUI()
        {
            float currentPrice = GetMarketPrice(selectedStockCode);
            int holdingQuantity = GetHoldingQuantity(selectedStockCode);

            if (stockNameText != null)
                stockNameText.text = selectedStockName;

            if (stockBalanceText != null)
                stockBalanceText.text = $"보유수량 : {holdingQuantity:N0}";

            if (currentPriceText != null)
                currentPriceText.text = $"현재가 : {currentPrice:N0}";
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
    }
}
