using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class StockHistoryExporter : MonoBehaviour
{
    [Header("Refs")]
    public MarketSimulator market;
    public GameStateController game;

    [Header("Output")]
    public string fileName = "stocks_history.json";

    [Header("Candle Settings")]
    public int ticksPerCandle = 5;
    public int keepCandles = 60; // 차트에 남길 캔들 수(예: 60개)

    [Header("Selected Stock")]
    public string selectedStockId; // 비면 자동으로 첫 종목 선택

    private int _tickCounter = 0;
    private int _candleIndex = 0;

    // 종목별로 현재 캔들 집계 상태 보관
    private class Acc
    {
        public bool has;
        public float open, high, low, close;
        public long volume;
    }

    private Dictionary<string, Acc> _acc = new();
    private Dictionary<string, List<Candle>> _candles = new();

    public void InitFromMarket()
    {
        _acc.Clear();
        _candles.Clear();

        if (market == null) return;

        foreach (var kv in market.GetAllStocks())
        {
            _acc[kv.Key] = new Acc();
            _candles[kv.Key] = new List<Candle>();
        }

        // 선택 종목 자동 설정
        if (string.IsNullOrEmpty(selectedStockId) && _candles.Keys.Count > 0)
            selectedStockId = _candles.Keys.First();

        _tickCounter = 0;
        _candleIndex = 0;

        Debug.Log($"[HistoryExporter] Init. stocks={_candles.Count}, selected={selectedStockId}");
    }

    // MarketSimulator.Tick() 끝에서 호출해주면 됨
    public void OnTick()
    {
        if (market == null) return;

        _tickCounter++;

        // 1) 모든 종목에 대해 현재 tick 가격을 캔들에 누적
        foreach (var kv in market.GetAllStocks())
        {
            var id = kv.Key;
            var price = kv.Value.currentPrice;

            if (!_acc.TryGetValue(id, out var a))
            {
                a = new Acc();
                _acc[id] = a;
            }

            if (!a.has)
            {
                a.has = true;
                a.open = a.high = a.low = a.close = price;
                a.volume = 0;
            }
            else
            {
                if (price > a.high) a.high = price;
                if (price < a.low)  a.low  = price;
                a.close = price;
            }

            
            a.volume += kv.Value.tickVolume;
        }

        // 2) 캔들 마감 시점(5틱)
        if (_tickCounter < ticksPerCandle) return;
        _tickCounter = 0;
        _candleIndex++;

        // 종목별 캔들 확정
        foreach (var id in _acc.Keys.ToList())
        {
            var a = _acc[id];
            if (!a.has) continue;

            // 팀원 Candle 모델이 tick/price/volume만 있으니까
            // price에는 close를 넣자 (차트 "CLOSE"로 쓰기)
            _candles[id].Add(new Candle
            {
                tick = _candleIndex,
                price = a.close,
                volume = a.volume
            });

            // keepCandles 유지
            var list = _candles[id];
            if (list.Count > keepCandles)
                list.RemoveRange(0, list.Count - keepCandles);

            // 다음 캔들 준비
            a.has = false;
            a.volume = 0;
        }

        // 3) UI용 json 갱신: 선택된 종목 1개만 stocks_history.json로 덮어쓰기
        WriteSelectedHistoryJson();
    }

    private void WriteSelectedHistoryJson()
    {
        if (string.IsNullOrEmpty(selectedStockId)) return;
        if (!_candles.TryGetValue(selectedStockId, out var list)) return;

        var data = new StockHistory10D
        {
            code = selectedStockId,
            name = selectedStockId,
            candles = list.ToList()
        };

        var path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(data, true), Encoding.UTF8);
        // Debug.Log($"[HistoryExporter] wrote: {path}");
    }

    // 나중에 UI 클릭에서 호출하면 됨
    public void SelectStock(string stockId)
    {
        if (string.IsNullOrEmpty(stockId)) return;
        if (!_candles.ContainsKey(stockId)) return;

        selectedStockId = stockId;
        WriteSelectedHistoryJson(); // 즉시 반영
        Debug.Log($"[HistoryExporter] selected={selectedStockId}");
    }
}