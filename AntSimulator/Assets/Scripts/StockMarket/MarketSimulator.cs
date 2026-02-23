using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;
using UnityEngine;
using System;

public class MarketSimulator : MonoBehaviour
{
    public List<StockDefinition> stockDefinitions;
    public GlobalCommunityManager globalCommunity;
    public HTSCommunityManager htsCommunity;

    private readonly Dictionary<string, StockState> stocks = new();
    private readonly Dictionary<string, StockDefinition> defMap = new();

    public float tickIntervalSec = 1f;
    private float timer;

    private int tickInDay = 0;
    public int CurrentTickInDay => tickInDay;
    public bool IsDayTickFinished => tickInDay >= Mathf.Max(1, ticksPerDay);

    public EventManager eventManager;
    public StockHistoryExporter historyExporter;

    public bool simulateTicks = false;
    public bool allowEventReveal = true;

    [Header("Volume")] 
    public int ticksPerDay;

    [Header("Recording")] public RunRecorder runRecorder;
    public GameStateController gameStateController;
    
    public event Action<EventDefinition, int, int> OnEventRevealed;

    void Start()
    {
        stocks.Clear();
        defMap.Clear();
        

        if (stockDefinitions != null)
        {
            foreach (var def in stockDefinitions)
            {
                if (def == null) continue;
                defMap[def.stockId] = def;
                stocks[def.stockId] = new StockState(def.stockId, def.basePrice);
                Debug.Log($"[DEF] {def.stockId} floatShares={def.floatShares} avgDailyVol={def.avgDailyVolume} ticksPerDay={ticksPerDay}");
            }
        }
        if(htsCommunity != null) htsCommunity.InitStocks(stocks.Keys);

        if (gameStateController != null)
            gameStateController.OnDayStarted += HandleDayStarted;
        
        historyExporter?.InitFromMarket();

        Debug.Log($"Initialized {stocks.Count} stocks");
    }

    public Dictionary<string, StockState> GetAllStocks() => stocks;

    private void Update()
    {
        if(!simulateTicks) return;
        
        timer += Time.deltaTime;
        if (timer < tickIntervalSec) return;
        timer -= tickIntervalSec;

        Tick();
    }

    private void Tick()
    {
        tickInDay++;

        if (allowEventReveal)
        {
            HandleEventRevealAndDelist(gameStateController != null ? gameStateController.currentDay : 1, tickInDay);
        }

        foreach (var kv in stocks)
            UpdateStock(kv.Value, defMap[kv.Key]);

        if (globalCommunity != null && gameStateController != null)
        {
            globalCommunity.OnTick(gameStateController.currentDay, tickInDay);
        }

        
        runRecorder?.RecordTick();
        historyExporter?.OnTick();
        eventManager?.OnTick();
    }

    private void UpdateStock(StockState stock, StockDefinition def)
    {
        if(stock.isDelisted)return;
        
        stock.prevPrice = stock.currentPrice;
        CalculateStockPrice(stock, def);
        CalculateStockVolume(stock, def);
        float pEvent = 0f, dEvent = 0f;
        if (eventManager != null)
            (pEvent, dEvent) = eventManager.GetEffectsForStock(stock.stockId, def.sector);

        bool hasEventEffect = Mathf.Abs(pEvent) > 0.0001f || Mathf.Abs(dEvent) > 0.0001f;

        if (htsCommunity != null && gameStateController != null)
        {
            htsCommunity.OnTick(
                gameStateController.currentDay,
                tickInDay,
                stock.stockId,
                stock.prevPrice,
                stock.currentPrice,
                stock.tickVolume,
                stock.emaVolume,
                stock.volatilityMultiplier,
                hasEventEffect,
                def.communitySensitivity
            );
        }
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
        maxChange += stock.volatilityMultiplier * 0.01f * def.noiseScale;

        // 최종 캡
        maxChange = Mathf.Clamp(maxChange, 0f, 0.30f);

        float change = UnityEngine.Random.Range(0f, maxChange);
        stock.currentPrice *= isUp ? (1f + change) : (1f - change);
        if (stock.currentPrice < def.basePrice*0.05) stock.currentPrice = def.basePrice*0.05f;
    }

    private void HandleDayStarted(int day)
    {
        tickInDay = 0;
        foreach (var s in stocks.Values)
        {
            CalculateDailyVolatility(s);
            s.volatilityMultiplier = 1f;
            s.todayVolume = 0;
            s.tickVolume = 0;
            s.eventVolBoost = 0f;
        }
        
        HandleEventRevealAndDelist(day, 0);

        if (globalCommunity != null)
        {
            globalCommunity.OnDayStarted(day);
        }

        if (htsCommunity != null)
        {
            htsCommunity.OnDayStarted(day);
        }
    }

    private void CalculateDailyVolatility(StockState stock)
    {
        
        stock.volatilityMultiplier = 1f;
    }

    private void CalculateStockVolume(StockState stock, StockDefinition def)
    {
        int tpd = Mathf.Max(1, ticksPerDay);

        float pEvent = 0f;
        float dEvent = 0f;
        if (eventManager != null)
        {
            (pEvent, dEvent) = eventManager.GetEffectsForStock(stock.stockId, def.sector);
        }

        //기본 틱 거래량
        double baseTick = (double)def.avgDailyVolume / tpd;
        
        //가격 변동이 클수록 거래량 증가
        float ret = Mathf.Abs((stock.currentPrice - stock.prevPrice) / Mathf.Max(0.0001f, stock.prevPrice));
        double boostByMove = 1.0 + (def.volumeBoostK * ret);
        
        //이벤트가 있으면 거래량도 늘어남
        double boostByEvent = 1.0;

        boostByEvent += (1.0 * def.eventToVolume * Mathf.Abs(dEvent));
        boostByEvent += (1.0 * def.eventToVolume * Mathf.Abs(pEvent));

        // 이벤트 있으면 최소 기본 버프
        if (Mathf.Abs(dEvent) > 0.0001f || Mathf.Abs(pEvent) > 0.0001f)
            boostByEvent *= 1.3; // 기본 30% 상승

        boostByEvent = System.Math.Min(boostByEvent, 5.0); // 상한선 
        
        //랜덤 노이즈 
        double noise = UnityEngine.Random.Range(def.volumeNoiseMin, def.volumeNoiseMax);
        double v = baseTick * boostByMove * boostByEvent * noise;

       
        
        //상한 컷
        double maxTick = def.floatShares * 0.03;
        v = System.Math.Min(v, maxTick);
        
        long tickVol = (long)System.Math.Max(0, System.Math.Round(v));

        stock.tickVolume = tickVol;
        stock.todayVolume += tickVol;

        //평소 거래량 추정
        if (stock.emaVolume <= 1f)
        {
            stock.emaVolume = (float)baseTick;
        }

        stock.emaVolume = Mathf.Lerp(stock.emaVolume, tickVol, 0.05f);
        
        //거래량 스파이크 -> 변동성 상승
        float rel = tickVol / Mathf.Max(1f, stock.emaVolume);
        float spike = Mathf.Max(0f, rel - 1f);
        float volFromVolume = 0.03f * Mathf.Log(1f + spike);
        
        //이벤트 -> 변동성 상승
        stock.eventVolBoost += def.eventToVolatility * Mathf.Abs(dEvent);

        stock.volatilityMultiplier = Mathf.Clamp(1f + stock.eventVolBoost + volFromVolume, 1f, 3.0f);
    }

    private void HandleEventRevealAndDelist(int day, int tick)
    {
        if(eventManager == null || eventManager.db == null) return;
        var actives = eventManager.ActiveEvents;
        if(actives == null) return;

        foreach (var inst in actives)
        {
            if(inst == null) continue;
            if(inst.revealed) continue;
            if (tick >= inst.revealTickInDay)
            {
                inst.revealed = true;
                var def = eventManager.db.FindById(inst.eventId);
                if(def == null) continue;
                
                Debug.Log($"[EVENT REVEAL] event={def.eventId} title={def.title} (D{day} T{tick})");
                OnEventRevealed?.Invoke(def, day, tick);
                if (def.delist && !inst.delistApplied)
                {
                    inst.delistApplied = true;
                    var targets = (def.delistStockIds != null && def.delistStockIds.Count > 0)
                        ? def.delistStockIds
                        : def.targetStockIds;
                    if (targets == null) continue;

                    foreach (var stockId in targets)
                    {
                        if (stocks.TryGetValue(stockId, out var s) && !s.isDelisted)
                        {
                            s.isDelisted = true;
                            Debug.Log($"[DELIST] {stockId} delisted by event={def.eventId}");
                        }
                    }
                }
            }
        }
    }
    
    public void ProcessEventRevealNow(int day, int tickInDay)
    {
        if (!allowEventReveal) return;
        HandleEventRevealAndDelist(day, tickInDay);
    }

    public void TickOnce()
    {
        Tick();
    }
}
