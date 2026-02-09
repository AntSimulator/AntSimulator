using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StockState
{
    public string stockId; //주식 분별자
    public float currentPrice; // 현재 가격
    public float prevPrice; // 직전 틱 가격
    public float volatilityMultiplier = 1f; // 변동성

    public long tickVolume;
    public long todayVolume;
    public float emaVolume; //평소 거래량 추정치
    public float eventVolBoost; // 이벤트로 쌓이는 변동성 부스트
    public List<float> priceHistroy = new ();

    public bool isDelisted;

    public StockState(string id, float startPrice)
    {
        stockId = id;
        currentPrice = startPrice;
        prevPrice = startPrice;
        
        priceHistroy.Add(startPrice);
    }

    public void Record()
    {
        priceHistroy.Add(currentPrice);
    }
}
