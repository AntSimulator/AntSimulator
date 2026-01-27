using System;
using UnityEngine;

[Serializable]
public class StockState
{
    public string stockId; //주식 분별자
    public string stockName; //주식 이름
    public float currentPrice; // 현재 가격
    public float prevPrice; // 직전 틱 가격
    public float volatilityMultiplier = 1f; // 변동성
    public float todayVolume; // 오늘 거래량

    public StockState(string id, float startPrice)
    {
        stockId = id;
        currentPrice = startPrice;
        prevPrice = startPrice;
    }
}
