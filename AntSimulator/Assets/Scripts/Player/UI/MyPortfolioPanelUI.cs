using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Player.Models;
using Player.Runtime;
using Stocks.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Player.UI
{
    [DisallowMultipleComponent]
    public sealed class MyPortfolioPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MarketSimulator marketSimulator;

        [Header("UI")]
        [SerializeField] private Transform portfolioListRoot;
        [SerializeField] private GameObject ownedStockRectPrefab;
        [SerializeField] private TMP_Text stockEvaluationText;
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private bool clearExistingChildrenOnInit = true;

        private const string PortfolioStockImgAssetPath = "Assets/Prefab/PortfolioStockImg.prefab";
        private const string ListRootName = "PortfolioList";
        private const string StockEvaluationNodeName = "StockEvaluationText";
        private const string CashNodeName = "CashText";

        private const string StockNameNode = "StockName";
        private const string QtyNode = "Qty";
        private const string CurrentPriceNode = "CurrentPrice";
        private const string TotalBuyQuantityNode = "TotalBuyQuantity";
        private const string AverageBuyPriceNode = "AverageBuyPrice";
        private const string ProfitRateNode = "ProfitRate";

        private readonly Dictionary<string, RowView> rowsByStockId = new();
        private readonly List<string> staleStockIds = new();
        private bool listInitialized;

        private sealed class RowView
        {
            public GameObject root;
            public TMP_Text stockNameText;
            public TMP_Text currentPriceText;
            public TMP_Text quantityText;
            public TMP_Text avgBuyPriceText;
            public TMP_Text profitRateText;
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureSummaryTexts();
            EnsureListRoot();
        }

        private void OnEnable()
        {
            RefreshView(forceRebuild: true);
            RegisterSummaryBindings();
        }

        private void OnDisable()
        {
            UnregisterSummaryBindings();
        }

        private void Update()
        {
            RefreshView(forceRebuild: false);
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

            if (ownedStockRectPrefab == null)
            {
                TryResolveOwnedStockRectPrefab();
            }
        }

        private void EnsureSummaryTexts()
        {
            if (stockEvaluationText == null)
            {
                var existing = transform.Find(StockEvaluationNodeName);
                stockEvaluationText = existing != null ? existing.GetComponent<TMP_Text>() : null;
            }

            if (stockEvaluationText == null)
            {
                stockEvaluationText = CreateSummaryText(
                    StockEvaluationNodeName,
                    new Vector2(16f, -16f),
                    "주식 평가금: 0");
            }

            if (cashText == null)
            {
                var existing = transform.Find(CashNodeName);
                cashText = existing != null ? existing.GetComponent<TMP_Text>() : null;
            }

            if (cashText == null)
            {
                cashText = CreateSummaryText(
                    CashNodeName,
                    new Vector2(16f, -56f),
                    "현재 현금: 0");
            }
        }

        private TMP_Text CreateSummaryText(string nodeName, Vector2 anchoredPosition, string defaultText)
        {
            var go = new GameObject(nodeName, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(440f, 32f);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            return text;
        }

        private void EnsureListRoot()
        {
            if (portfolioListRoot == null)
            {
                var existing = transform.Find(ListRootName);
                portfolioListRoot = existing;
            }

            if (portfolioListRoot != null)
            {
                ConfigureListRootRect(portfolioListRoot);
                EnsureVerticalLayout(portfolioListRoot);
                if (clearExistingChildrenOnInit && !listInitialized)
                {
                    ClearExistingListChildren();
                }
                listInitialized = true;
                return;
            }

            var listRoot = new GameObject(ListRootName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            var rect = listRoot.GetComponent<RectTransform>();
            rect.SetParent(transform, false);

            portfolioListRoot = rect;
            ConfigureListRootRect(portfolioListRoot);
            EnsureVerticalLayout(portfolioListRoot);
            if (clearExistingChildrenOnInit && !listInitialized)
            {
                ClearExistingListChildren();
            }
            listInitialized = true;
        }

        private static void ConfigureListRootRect(Transform target)
        {
            if (!(target is RectTransform rect))
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(16f, 16f);
            rect.offsetMax = new Vector2(-16f, -104f);
        }

        private void ClearExistingListChildren()
        {
            if (portfolioListRoot == null)
            {
                return;
            }

            for (var i = portfolioListRoot.childCount - 1; i >= 0; i--)
            {
                var child = portfolioListRoot.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private static void EnsureVerticalLayout(Transform target)
        {
            var layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void RefreshView(bool forceRebuild)
        {
            if (playerController == null || marketSimulator == null || portfolioListRoot == null)
            {
                ResolveReferences();
                EnsureSummaryTexts();
                EnsureListRoot();
            }

            if (playerController == null || marketSimulator == null || portfolioListRoot == null)
            {
                return;
            }

            var holdings = playerController.GetHoldings();
            var marketStocks = marketSimulator.GetAllStocks();

            staleStockIds.Clear();
            foreach (var stockId in rowsByStockId.Keys)
            {
                staleStockIds.Add(stockId);
            }

            foreach (var pair in holdings)
            {
                var stockId = pair.Key;
                var holding = pair.Value;
                if (holding == null || holding.quantity <= 0 || string.IsNullOrWhiteSpace(stockId))
                {
                    continue;
                }

                var currentPrice = GetCurrentPrice(stockId, holding.avgBuyPrice, marketStocks);
                var stockEvaluation = (long)Math.Round(currentPrice * holding.quantity);
                var profitRate = CalculateProfitRate(currentPrice, holding.avgBuyPrice);

                var rowView = GetOrCreateRow(stockId, forceRebuild);
                UpdateRow(rowView, stockId, currentPrice, holding.quantity, holding.avgBuyPrice, stockEvaluation, profitRate);
                staleStockIds.Remove(stockId);
            }

            for (var i = 0; i < staleStockIds.Count; i++)
            {
                var stockId = staleStockIds[i];
                if (!rowsByStockId.TryGetValue(stockId, out var staleRow))
                {
                    continue;
                }

                if (staleRow.root != null)
                {
                    Destroy(staleRow.root);
                }

                rowsByStockId.Remove(stockId);
            }

        }

        private float GetCurrentPrice(string stockId, float fallbackPrice, IReadOnlyDictionary<string, StockState> marketStocks)
        {
            if (marketStocks != null && marketStocks.TryGetValue(stockId, out var state))
            {
                return state.currentPrice;
            }

            return Mathf.Max(0f, fallbackPrice);
        }

        private static float CalculateProfitRate(float currentPrice, float avgBuyPrice)
        {
            if (avgBuyPrice <= 0f)
            {
                return 0f;
            }

            return (currentPrice - avgBuyPrice) / avgBuyPrice * 100f;
        }

        private RowView GetOrCreateRow(string stockId, bool forceRebuild)
        {
            if (rowsByStockId.TryGetValue(stockId, out var existing) && existing.root != null && !forceRebuild)
            {
                return existing;
            }

            if (existing != null && existing.root != null)
            {
                Destroy(existing.root);
            }

            var row = BuildRowView();
            rowsByStockId[stockId] = row;
            return row;
        }

        private RowView BuildRowView()
        {
            var rowRoot = InstantiateRowRoot();
            if (rowRoot == null)
            {
                rowRoot = CreateFallbackRowRoot();
            }

            if (rowRoot.transform.parent != portfolioListRoot)
            {
                rowRoot.transform.SetParent(portfolioListRoot, false);
            }

            var layoutElement = rowRoot.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rowRoot.AddComponent<LayoutElement>();
            }

            var preferredHeight = 56f;
            if (rowRoot.TryGetComponent<RectTransform>(out var rect))
            {
                preferredHeight = Mathf.Max(56f, rect.rect.height + 8f);
            }

            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = 0f;

            return new RowView
            {
                root = rowRoot,
                stockNameText = FindTextAny(rowRoot.transform, StockNameNode),
                currentPriceText = FindTextAny(rowRoot.transform, CurrentPriceNode),
                quantityText = FindTextAny(rowRoot.transform, TotalBuyQuantityNode, QtyNode),
                avgBuyPriceText = FindTextAny(rowRoot.transform, AverageBuyPriceNode),
                profitRateText = FindTextAny(rowRoot.transform, ProfitRateNode)
            };
        }

        private GameObject InstantiateRowRoot()
        {
            TryResolveOwnedStockRectPrefab();
            return ownedStockRectPrefab != null
                ? Instantiate(ownedStockRectPrefab, portfolioListRoot)
                : null;
        }

        private void TryResolveOwnedStockRectPrefab()
        {
            if (ownedStockRectPrefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            ownedStockRectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortfolioStockImgAssetPath);
#endif
        }

        private GameObject CreateFallbackRowRoot()
        {
            var root = new GameObject("OwnedStockRect", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(portfolioListRoot, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 148f);

            var image = root.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateFallbackText(root.transform, StockNameNode, 20);
            CreateFallbackText(root.transform, CurrentPriceNode, 18);
            CreateFallbackText(root.transform, TotalBuyQuantityNode, 18);
            CreateFallbackText(root.transform, AverageBuyPriceNode, 18);
            CreateFallbackText(root.transform, ProfitRateNode, 18);

            return root;
        }

        private static TMP_Text CreateFallbackText(Transform parent, string nodeName, float fontSize)
        {
            var textGo = new GameObject(nodeName, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = textGo.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 30f);

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            text.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            return text;
        }

        private void UpdateRow(
            RowView row,
            string stockId,
            float currentPrice,
            int quantity,
            float avgBuyPrice,
            long stockEvaluation,
            float profitRate)
        {
            var displayName = ResolveStockDisplayName(stockId);

            if (row.stockNameText != null)
            {
                row.stockNameText.text = $"{displayName} ({stockId})";
            }

            if (row.currentPriceText != null)
            {
                row.currentPriceText.text = $"평가금: {stockEvaluation:N0}";
            }

            if (row.quantityText != null)
            {
                row.quantityText.text =
                    $"평가금: {stockEvaluation:N0} | 수량: {quantity:N0} | 평균가: {avgBuyPrice:N2} | 손익: {profitRate:+0.00;-0.00;0.00}%";
            }

            if (row.avgBuyPriceText != null)
            {
                row.avgBuyPriceText.text = $"현재가/평균가: {currentPrice:N0} / {avgBuyPrice:N2}";
            }

            if (row.profitRateText != null)
            {
                row.profitRateText.text = $"손익: {profitRate:+0.00;-0.00;0.00}%";
                row.profitRateText.color = profitRate >= 0f
                    ? new Color(0.93f, 0.25f, 0.25f, 1f)
                    : new Color(0.22f, 0.58f, 0.95f, 1f);
            }
        }

        private string ResolveStockDisplayName(string stockId)
        {
            if (string.IsNullOrWhiteSpace(stockId) || marketSimulator == null || marketSimulator.stockDefinitions == null)
            {
                return stockId ?? string.Empty;
            }

            for (var i = 0; i < marketSimulator.stockDefinitions.Count; i++)
            {
                var definition = marketSimulator.stockDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (!string.Equals(definition.stockId, stockId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return string.IsNullOrWhiteSpace(definition.displayName)
                    ? stockId
                    : definition.displayName;
            }

            return stockId;
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

        private static TMP_Text FindTextAny(Transform root, params string[] nodeNames)
        {
            if (nodeNames == null || nodeNames.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < nodeNames.Length; i++)
            {
                var text = FindText(root, nodeNames[i]);
                if (text != null)
                {
                    return text;
                }
            }

            return null;
        }

        private void RegisterSummaryBindings()
        {
            if (PlayerStatusUI.Instance == null) return;

            if (stockEvaluationText != null)
                PlayerStatusUI.Instance.RegisterBinding(StatusType.StockEvaluation, stockEvaluationText, "주식 평가금: {0:N0}");

            if (cashText != null)
                PlayerStatusUI.Instance.RegisterBinding(StatusType.Cash, cashText, "현재 현금: {0:N0}");
        }

        private void UnregisterSummaryBindings()
        {
            if (PlayerStatusUI.Instance == null) return;

            if (stockEvaluationText != null)
                PlayerStatusUI.Instance.UnregisterBinding(StatusType.StockEvaluation, stockEvaluationText);

            if (cashText != null)
                PlayerStatusUI.Instance.UnregisterBinding(StatusType.Cash, cashText);
        }

        private void OnDestroy()
        {
            UnregisterSummaryBindings();
            rowsByStockId.Clear();
            staleStockIds.Clear();
        }
    }
}
