using UnityEngine;
using TMPro;

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

    [Header("UI (TMP)")]
    [SerializeField] private TMP_Dropdown stockDropdown;
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text qtyText;

    private PlayerState state;
    private int selectedIndex = 0;

    private TestStock CurrentStock
        => (testStocks != null && testStocks.Length > 0)
            ? testStocks[Mathf.Clamp(selectedIndex, 0, testStocks.Length - 1)]
            : null;

    private void Awake()
    {
        state = new PlayerState(startCash);

        // 최소 안전장치: 테스트 종목이 비어있으면 기본 1개 생성
        if (testStocks == null || testStocks.Length == 0)
        {
            testStocks = new[] { new TestStock { stockId = "ANT_CO", currentPrice = 1000 } };
        }

        SetupDropdown();
        UpdateUI();
    }

    private void SetupDropdown()
    {
        if (stockDropdown == null) return;

        stockDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < testStocks.Length; i++)
        {
            options.Add(testStocks[i].stockId);
        }
        stockDropdown.AddOptions(options);

        stockDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        stockDropdown.onValueChanged.AddListener(OnDropdownChanged);

        selectedIndex = Mathf.Clamp(stockDropdown.value, 0, testStocks.Length - 1);
    }

    private void OnDropdownChanged(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, testStocks.Length - 1);
        UpdateUI();
    }

    public void Buy()
    {
        var cur = CurrentStock;
        if (cur == null) return;

        if (!state.TryBuy(cur.stockId, qtyStep, cur.currentPrice, out var reason))
            Debug.LogWarning($"[BUY FAIL] {reason}");
        else
            Debug.Log($"[BUY] {cur.stockId} x{qtyStep} @ {cur.currentPrice}");

        UpdateUI();
    }

    public void Sell()
    {
        var cur = CurrentStock;
        if (cur == null) return;

        if (!state.TrySell(cur.stockId, qtyStep, cur.currentPrice, out var reason))
            Debug.LogWarning($"[SELL FAIL] {reason}");
        else
            Debug.Log($"[SELL] {cur.stockId} x{qtyStep} @ {cur.currentPrice}");

        UpdateUI();
    }

    public void PriceUp()
    {
        var cur = CurrentStock;
        if (cur == null) return;

        cur.currentPrice += priceStep;
        UpdateUI();
    }

    public void PriceDown()
    {
        var cur = CurrentStock;
        if (cur == null) return;

        cur.currentPrice = Mathf.Max(1, cur.currentPrice - priceStep);
        UpdateUI();
    }

    private void UpdateUI()
    {
        var cur = CurrentStock;

        if (cashText != null) cashText.text = $"Cash: {state.cash}";

        if (cur == null)
        {
            if (priceText != null) priceText.text = "Price: -";
            if (qtyText != null) qtyText.text = "Qty: -";
            return;
        }

        if (priceText != null) priceText.text = $"Price: {cur.currentPrice}";
        if (qtyText != null) qtyText.text = $"Qty: {state.GetQuantity(cur.stockId)}";
    }
    
    public long Cash
    {
        get => state != null ? state.cash : 0;
        set
        {
            if (state == null) return;
            state.cash = value;
            UpdateUI(); // 은행 이체로 돈이 변하면 UI도 즉시 갱신
        }
        //new
    }
}