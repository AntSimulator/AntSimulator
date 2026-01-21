using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem.Controls;

public class MarketSimulator : MonoBehaviour
{
    //TODO: make a dictionary to save each stock
    //TODO: link with stockDef
    
    [Header("Tick")] public float tickIntervalSec = 1.0f;

    [Header("Simulation Parameters")] 
    [Range(-50, 50)] public float baseDrift = 0.0f; // 기본적인 시장의 방 (양수면 기본적으로 올라감. 음수면 내려감. 물론 방향성인거지 다른 영향으로 drift와 다르게 흘러갈 수 잇음)
    public float noiseScale = 1.0f; //가격 변화에 들어갈 랜덤성을 얼마나 크게 할지 정하는 변수. noise가 크면 클수록 예측 불가능성 커짐
    public float eventScale = 1.0f;
    public float maxUpPercentPerTick = 0.08f;
    public float maxDownPercentPerTick = 0.08f;
    public float minPrice = 1f;
    
    
    //TODO change this variable with actual stock price
    private float stockPrice = 10.0f;

    private float timer = 0f;

    void Start()
    {
        //TODO: reference StockDefinition to create StockState
        
        //TODO: log Initialized N Stocks
        
        //TODO: if stock price <= 0 -> stock price = 1
    }

    void Update()
    {
        //TODO: update every stock's information every tick

        timer += Time.deltaTime;
        if(timer < tickIntervalSec) return;
        timer -= tickIntervalSec;

        Tick();
    }

    private void Tick()
    {
        //TODO: for each stock, save previous price, create logic for stock market
        
        //TODO: log print out changes of each stock per tick
    }

    // Tick 안에서 얘 호출하면 값 바뀜
    private void calculateStockPrice()
    {
        float rdNum = UnityEngine.Random.Range(-10f, 11f)*noiseScale;

        stockPrice = stockPrice * (1+Mathf.Clamp((baseDrift + rdNum + eventScale), maxDownPercentPerTick, maxUpPercentPerTick));
    }
}
