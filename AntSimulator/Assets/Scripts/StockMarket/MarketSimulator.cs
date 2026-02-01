using System.Collections.Generic;
using UnityEngine;

public class MarketSimulator : MonoBehaviour
{
    public List<StockDefinition> stockDefinitions;

    private readonly Dictionary<string, StockState> stocks = new();
    private readonly Dictionary<string, StockDefinition> defMap = new();

    public float tickIntervalSec = 1f;
    private float timer;

    public EventManager eventManager;
    public StockHistoryExporter historyExporter;

    [Header("Recording")] public RunRecorder runRecorder;
    public GameStateController gameStateController;

    void Start()
    {
        stocks.Clear();
        defMap.Clear();

        if (stockDefinitions != null)
        {
            foreach (var def in stockDefinitions)
            {
                if (def == null) continue;
                defMap[def.name] = def;
                stocks[def.name] = new StockState(def.name, def.basePrice);
            }
        }

        if (gameStateController != null)
            gameStateController.OnDayStarted += HandleDayStarted;
        
        historyExporter?.InitFromMarket();

        Debug.Log($"Initialized {stocks.Count} stocks");
    }

    public Dictionary<string, StockState> GetAllStocks() => stocks;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < tickIntervalSec) return;
        timer -= tickIntervalSec;

        Tick();
    }

    private void Tick()
    {
        foreach (var kv in stocks)
            UpdateStock(kv.Value, defMap[kv.Key]);

        runRecorder?.RecordTick();
        historyExporter?.OnTick();
    }

    private void UpdateStock(StockState stock, StockDefinition def)
    {
        stock.prevPrice = stock.currentPrice;
        CalculateStockPrice(stock, def);
        stock.Record();
    }

    private void CalculateStockPrice(StockState stock, StockDefinition def)
    {
        // 이벤트 효과
        float pEvent = 0f;
        float dEvent = 0f;
        if (eventManager != null)
            (pEvent, dEvent) = eventManager.GetEffectsForStock(stock.stockId, def.sector);

        // 확률: 50% 기준에서 퍼센트포인트 더하기
        float upThreshold = 50f + (pEvent * 100f);
        upThreshold = Mathf.Clamp(upThreshold, 0f, 100f);

        float downThreshold = 100f - upThreshold;
        float roll = UnityEngine.Random.Range(1f, 100f);
        bool isUp = (roll > downThreshold);

        // 기본 캡
        float maxChange = isUp ? def.maxUpPercent : def.maxDownPercent;

        // 이벤트 폭 증가분 더하기 (dEvent는 증가분)
        maxChange = maxChange + dEvent;

        // 변동성(선택)
        maxChange += stock.volatilityMultiplier * 0.01f;

        // 최종 캡
        maxChange = Mathf.Clamp(maxChange, 0f, 0.30f);

        float change = UnityEngine.Random.Range(0f, maxChange);
        stock.currentPrice *= isUp ? (1f + change) : (1f - change);
        if (stock.currentPrice < 1f) stock.currentPrice = 1f;
    }

    private void HandleDayStarted(int day)
    {
        foreach (var s in stocks.Values)
            CalculateDailyVolatility(s);
    }

    private void CalculateDailyVolatility(StockState stock)
    {
        
        stock.volatilityMultiplier = 1f;
    }
}