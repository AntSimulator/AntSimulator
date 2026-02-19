using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Player.Runtime;

namespace Player.UI
{
    public enum StatusType
    {
        Cash,
        Hp,
        StockEvaluation,
        SelectedQuantity,
        SelectedAvgBuyPrice,
        SelectedProfitRate
    }

    public class PlayerStatusUI : MonoBehaviour
    {
        public static PlayerStatusUI Instance { get; private set; }

        [Serializable]
        public class StatusBinding
        {
            public StatusType statusType;
            public TMP_Text targetText;
            public string format;
        }

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MarketSimulator marketSimulator;

        [Header("Bindings")]
        [SerializeField] private List<StatusBinding> bindings = new();

        [Header("Polling")]
        [SerializeField] private float pollingInterval = 0.5f;

        private readonly List<StatusBinding> runtimeBindings = new();
        private float pollingTimer;

        public static bool IsPollingRequired(StatusType type)
        {
            return type == StatusType.StockEvaluation || type == StatusType.SelectedProfitRate;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PlayerStatusUI] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            pollingTimer += Time.deltaTime;
            if (pollingTimer < pollingInterval) return;
            pollingTimer = 0f;

            CleanupNullTargets();
            UpdatePollingBindings();
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

        private void SubscribeEvents()
        {
            if (playerController == null) return;
            playerController.OnCashChanged += HandleCashChanged;
            playerController.OnHoldingsChanged += HandleHoldingsChanged;
            playerController.OnSelectedStockChanged += HandleSelectedStockChanged;
            playerController.OnHpChanged += HandleHpChanged;
        }

        private void UnsubscribeEvents()
        {
            if (playerController == null) return;
            playerController.OnCashChanged -= HandleCashChanged;
            playerController.OnHoldingsChanged -= HandleHoldingsChanged;
            playerController.OnSelectedStockChanged -= HandleSelectedStockChanged;
            playerController.OnHpChanged -= HandleHpChanged;
        }

        public void RegisterBinding(StatusType type, TMP_Text target, string format = null)
        {
            if (target == null) return;

            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].statusType == type && bindings[i].targetText == target)
                    return;
            }

            for (int i = 0; i < runtimeBindings.Count; i++)
            {
                if (runtimeBindings[i].statusType == type && runtimeBindings[i].targetText == target)
                    return;
            }

            runtimeBindings.Add(new StatusBinding
            {
                statusType = type,
                targetText = target,
                format = format
            });

            UpdateSingleBinding(type, target, format);
        }

        public void UnregisterBinding(StatusType type, TMP_Text target)
        {
            if (target == null) return;

            for (int i = runtimeBindings.Count - 1; i >= 0; i--)
            {
                if (runtimeBindings[i].statusType == type && runtimeBindings[i].targetText == target)
                {
                    runtimeBindings.RemoveAt(i);
                }
            }
        }

        private void CleanupNullTargets()
        {
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                if (bindings[i].targetText == null)
                    bindings.RemoveAt(i);
            }

            for (int i = runtimeBindings.Count - 1; i >= 0; i--)
            {
                if (runtimeBindings[i].targetText == null)
                    runtimeBindings.RemoveAt(i);
            }
        }

        private void RefreshAll()
        {
            foreach (StatusType type in Enum.GetValues(typeof(StatusType)))
            {
                UpdateBindingsForType(type);
            }
        }

        private void UpdatePollingBindings()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (IsPollingRequired(bindings[i].statusType))
                    UpdateSingleBinding(bindings[i]);
            }

            for (int i = 0; i < runtimeBindings.Count; i++)
            {
                if (IsPollingRequired(runtimeBindings[i].statusType))
                    UpdateSingleBinding(runtimeBindings[i]);
            }
        }

        private void UpdateBindingsForType(StatusType type)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].statusType == type)
                    UpdateSingleBinding(bindings[i]);
            }

            for (int i = 0; i < runtimeBindings.Count; i++)
            {
                if (runtimeBindings[i].statusType == type)
                    UpdateSingleBinding(runtimeBindings[i]);
            }
        }

        private void UpdateSingleBinding(StatusBinding binding)
        {
            if (binding.targetText == null) return;
            UpdateSingleBinding(binding.statusType, binding.targetText, binding.format);
        }

        private void UpdateSingleBinding(StatusType type, TMP_Text target, string format)
        {
            if (target == null) return;
            var newText = ComputeFormattedValue(type, format);
            if (target.text != newText)
            {
                target.text = newText;
            }
        }

        private string ComputeFormattedValue(StatusType type, string format)
        {
            switch (type)
            {
                case StatusType.Cash:
                {
                    var cash = playerController != null ? playerController.Cash : 0;
                    return string.IsNullOrEmpty(format) ? $"{cash:N0}" : string.Format(format, cash);
                }
                case StatusType.Hp:
                {
                    var cur = playerController != null ? playerController.CurrentHp : 0;
                    var max = playerController != null ? playerController.MaxHp : 0;
                    return string.IsNullOrEmpty(format) ? $"{cur}/{max}" : string.Format(format, cur, max);
                }
                case StatusType.StockEvaluation:
                {
                    var eval = ComputeTotalStockEvaluation();
                    return string.IsNullOrEmpty(format) ? $"{eval:N0}" : string.Format(format, eval);
                }
                case StatusType.SelectedQuantity:
                {
                    var qty = playerController != null ? playerController.GetSelectedQuantity() : 0;
                    return string.IsNullOrEmpty(format) ? $"{qty}" : string.Format(format, qty);
                }
                case StatusType.SelectedAvgBuyPrice:
                {
                    var avg = playerController != null ? playerController.GetSelectedAvgBuyPrice() : 0f;
                    return string.IsNullOrEmpty(format) ? $"{avg:N2}" : string.Format(format, avg);
                }
                case StatusType.SelectedProfitRate:
                {
                    var rate = ComputeSelectedProfitRate();
                    return string.IsNullOrEmpty(format) ? $"{rate:+0.00;-0.00;0.00}%" : string.Format(format, rate);
                }
                default:
                    return string.Empty;
            }
        }

        private long ComputeTotalStockEvaluation()
        {
            if (playerController == null || marketSimulator == null) return 0;

            var holdings = playerController.GetHoldings();
            var stocks = marketSimulator.GetAllStocks();
            if (holdings == null || stocks == null) return 0;

            long total = 0;
            foreach (var pair in holdings)
            {
                if (pair.Value == null || pair.Value.quantity <= 0) continue;
                float price = pair.Value.avgBuyPrice;
                if (stocks.TryGetValue(pair.Key, out var stockState))
                {
                    price = stockState.currentPrice;
                }

                total += (long)Math.Round(price * pair.Value.quantity);
            }

            return total;
        }

        private float ComputeSelectedProfitRate()
        {
            if (playerController == null || marketSimulator == null) return 0f;

            var stockId = playerController.SelectedStockId;
            if (string.IsNullOrEmpty(stockId)) return 0f;

            var avg = playerController.GetSelectedAvgBuyPrice();
            if (avg <= 0f) return 0f;

            var stocks = marketSimulator.GetAllStocks();
            if (stocks == null || !stocks.TryGetValue(stockId, out var stockState)) return 0f;

            return (stockState.currentPrice - avg) / avg * 100f;
        }

        private void HandleCashChanged(long cash)
        {
            UpdateBindingsForType(StatusType.Cash);
        }

        private void HandleHoldingsChanged()
        {
            UpdateBindingsForType(StatusType.SelectedQuantity);
            UpdateBindingsForType(StatusType.SelectedAvgBuyPrice);
            UpdateBindingsForType(StatusType.StockEvaluation);
        }

        private void HandleSelectedStockChanged(string stockId)
        {
            UpdateBindingsForType(StatusType.SelectedQuantity);
            UpdateBindingsForType(StatusType.SelectedAvgBuyPrice);
            UpdateBindingsForType(StatusType.SelectedProfitRate);
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            UpdateBindingsForType(StatusType.Hp);
        }
    }
}
