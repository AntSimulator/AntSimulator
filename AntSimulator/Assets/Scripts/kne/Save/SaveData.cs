using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StockSaveData
{
    public string stockId;
    public int amount;
}

[System.Serializable]
public class StockPriceData
{
    public string stockId;
    public float currentPrice;
}


[System.Serializable]
public class SaveData
{
    public int day;
    public float timer;
    public string stateName;
    public string saveTime;
    public string sceneName;
    public long saveCash;
    public List<StockSaveData> saveStocks = new List<StockSaveData>();
    public List<StockPriceData> marketPrices = new List<StockPriceData>();

}
