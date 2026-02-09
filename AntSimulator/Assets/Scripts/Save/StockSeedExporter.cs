using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Stocks.Models;

public class StockSeedExporter : MonoBehaviour
{
    [Header("Source")]
    public List<StockDefinition> stockDefinitions;

    [Header("Output")]
    public string fileName = "stocks_seed.json";

    public void WriteSeedOnce()
    {
        if (stockDefinitions == null || stockDefinitions.Count == 0)
        {
            Debug.LogError("[SeedExporter] stockDefinitions is empty.");
            return;
        }

        var db = new StockSeedDatabase();
        foreach (var def in stockDefinitions)
        {
            if (def == null) continue;

            db.stocks.Add(new StockSeedItem
            {
                code = def.name,      //HTS 코드로 사용 
                name = def.displayName,      
                iconColor = "#FFFFFF" // 일단 흰색.
            });
        }

        var path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(db, true), Encoding.UTF8);
        Debug.Log($"[SeedExporter] wrote: {path} ({db.stocks.Count} stocks)");
    }
}