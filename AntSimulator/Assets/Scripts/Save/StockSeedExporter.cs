using System.Collections.Generic;
using UnityEngine;
using Stocks.Models;

public class StockSeedExporter : MonoBehaviour
{
    [Header("Source")]
    public List<StockDefinition> stockDefinitions = new();

    [Header("Seed Defaults")]
    public long currentBalance = 100000;
    public int initialAmount = 0;
    public string iconColor = "#FFFFFF";

    public StockSeedDatabase BuildSeedDatabase()
    {
        if (stockDefinitions == null || stockDefinitions.Count == 0)
        {
            Debug.LogError("[SeedExporter] stockDefinitions is empty.");
            return null;
        }

        return StockSeedFactory.BuildFromDefinitions(stockDefinitions, currentBalance, initialAmount, iconColor);
    }

    public void WriteSeedOnce()
    {
        var db = BuildSeedDatabase();
        if (db == null)
        {
            return;
        }

        Debug.Log($"[SeedExporter] JSON export removed. Built seed from SO count={db.stocks.Count}");
    }
}
