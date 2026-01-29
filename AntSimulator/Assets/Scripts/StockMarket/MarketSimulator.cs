using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem.Controls;
using Random = Unity.Mathematics.Random;

public class MarketSimulator : MonoBehaviour
{
   public List<StockDefinition> stockDefinitions;
   private Dictionary<string, StockState> stocks = new();
   private Dictionary<string, StockDefinition> defMap = new();

   public float tickIntervalSec = 1f;
   private float timer;

   void Start()
   {
      foreach (var def in stockDefinitions)
      {
         defMap[def.name] = def;
         stocks[def.name] = new StockState(def.name, def.basePrice);
      }
      Debug.Log($"Initialized {stocks.Count} stocks");
      
   }

   private void Update()
   {
      timer += Time.deltaTime;
      if(timer < tickIntervalSec) return;
      timer -= tickIntervalSec;

      Tick();
   }

   private void Tick()
   {
      foreach (var kv in stocks)
      {
         UpdateStock(kv.Value, defMap[kv.Key]);
      }
   }

   private void UpdateStock(StockState stock, StockDefinition def)
   {
      stock.prevPrice = stock.currentPrice;
      CalculateStockPrice(stock, def);
      stock.Record();
   }

   private float CalculateDirection(StockDefinition def)
   {
      float score = 0f;

      return score;
   }

   private float CalculateMagnitude(StockState stock, StockDefinition def)
   {
      float baseMag = 1f;

      float mag = baseMag;
      return mag;
   }

   private void CalculateStockPrice(StockState stock, StockDefinition def)
   {
      /*
       * 기본적으로 올라갈 확률 반반. 이 확률에 이벤트, 선언, 뉴스 의 가중치를 주어서
       * 올라갈 확률을 조정해줌. 그 이후에 조정된 값을 바탕으로
       * 0 부터 100 까지의 수 하나를 랜덤으로 뽑은 뒤, 그게 downThreshold 보다 작으면
       * 해당 주식은 내려가고, 크면 해당 주식은 올라간다.
       */
      float upThreshold = 50;
      upThreshold = upThreshold
                    * def.eventProbWeight
                    * def.statementWeight
                    * def.newsWeight;
      upThreshold = Mathf.Clamp(upThreshold, 0f, 100.0f);
      float downThreshold = 100.0f - upThreshold;
      float roll = UnityEngine.Random.Range(1.0f, 100.0f);
      bool isUp = (roll > downThreshold);

      float maxChange;
      if (isUp == true)
      {
         maxChange = def.maxUpPercent;
      }
      else
      {
         maxChange = def.maxDownPercent;
      }

      maxChange = maxChange
                  * def.communityWeight
                  * def.eventDepthWeight;
      maxChange += stock.volatilityMultiplier * 0.01f;
      if (maxChange > 0.5)
      {
         maxChange = Mathf.Clamp(maxChange, 0, 0.30f);
      }

      float change = UnityEngine.Random.Range(0f, maxChange);

      if (isUp)
      {
         stock.currentPrice *= (1f + change);
      }
      else
      {
         stock.currentPrice *= (1f - change);
      }

      

   }
}
