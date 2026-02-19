using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
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
        [SerializeField] private TMP_Text currentPriceText;

        [Header("Input")]
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private bool disableInteractionWhenHidden = true;

        private string selectedStockCode;
        private string selectedStockName;
        private CanvasGroup panelCanvasGroup;

        private void Awake()
        {
            EnsureCanvasGroup();
        }

        private void OnEnable()
        {
            StockSelectionEvents.OnStockSelected += HandleStockSelected;
            PopupPanelSwitcher.OnPanelChanged += HandlePopupPanelChanged;

            if (quantityInput != null)
                quantityInput.onEndEdit.AddListener(OnQuantityInputChanged);

            RefreshInteractionGate();
        }

        private void OnDisable()
        {
            StockSelectionEvents.OnStockSelected -= HandleStockSelected;
            PopupPanelSwitcher.OnPanelChanged -= HandlePopupPanelChanged;

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

        private void HandlePopupPanelChanged(int _)
        {
            RefreshInteractionGate();
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(selectedStockCode)) return;

            UpdateUI();
        }

        private void UpdateUI()
        {
            float currentPrice = GetMarketPrice(selectedStockCode);

            if (stockNameText != null)
                stockNameText.text = selectedStockName;

            if (currentPriceText != null)
                currentPriceText.text = $"{currentPrice:N0}";
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

        private void RefreshInteractionGate()
        {
            if (!disableInteractionWhenHidden)
            {
                return;
            }

            EnsureCanvasGroup();
            if (panelCanvasGroup == null)
            {
                return;
            }

            bool canInteract = IsVisibleForInteraction();
            panelCanvasGroup.interactable = canInteract;
            panelCanvasGroup.blocksRaycasts = canInteract;

            if (!canInteract)
            {
                DeselectIfFocused();
            }
        }

        private bool IsVisibleForInteraction()
        {
            if (!gameObject.activeInHierarchy)
            {
                return false;
            }

            var parentGroups = GetComponentsInParent<CanvasGroup>(true);
            for (int i = 0; i < parentGroups.Length; i++)
            {
                var group = parentGroups[i];
                if (group == null || group == panelCanvasGroup)
                {
                    continue;
                }

                if (!group.interactable || !group.blocksRaycasts || group.alpha <= 0.001f)
                {
                    return false;
                }
            }

            return true;
        }

        private void DeselectIfFocused()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            {
                return;
            }

            if (EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void EnsureCanvasGroup()
        {
            if (panelCanvasGroup != null)
            {
                return;
            }

            panelCanvasGroup = GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}
