using System;
using System.IO;
using System.Text;
using UnityEngine;

public class RunRecorder : MonoBehaviour
{
    [Header("Ref")]
    public GameStateController gameStateController;
    public MarketSimulator marketSimulator;
    public EventManager eventManager;

    [Header("Run Settings")]
    public int totalDays = 4;
    public string runsFolderName = "runs";

    public string RunId;
    public string RunFolderPath;
    public string ManifestPath;
    public string TickHistoryPath;

    private int tickIndex = 0;
    private StreamWriter historyWriter;

    private void OnDestroy() => CloseWriter();

    private void CloseWriter()
    {
        if (historyWriter == null) return;
        historyWriter.Flush();
        historyWriter.Dispose();
        historyWriter = null;
    }

    public void BeginRunAndWriteManifest()
    {
        CloseWriter();

        if (marketSimulator == null)
        {
            Debug.LogError("[RunRecorder] marketSimulator is not assigned");
            return;
        }
        if (eventManager == null)
        {
            Debug.LogError("[RunRecorder] eventManager is not assigned");
            return;
        }

        RunId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        RunFolderPath = Path.Combine(Application.persistentDataPath, runsFolderName, RunId);
        Directory.CreateDirectory(RunFolderPath);

        ManifestPath = Path.Combine(RunFolderPath, "run_manifest.json");
        TickHistoryPath = Path.Combine(RunFolderPath, "tick_history.ndjson");

        var manifest = new RunManifestData
        {
            runId = RunId,
            totalDays = totalDays,
            createdAtUtc = DateTime.UtcNow.ToString("o")
        };

        foreach (var def in marketSimulator.stockDefinitions)
        {
            if (def == null) continue;
            manifest.stocks.Add(new RunManifestStock
            {
                stockId = def.name,
                basePrice = def.basePrice
            });
        }

        foreach (var inst in eventManager.GetRunEvents())
        {
            if (inst == null) continue;
            manifest.events.Add(new RunManifestEvent
            {
                eventId = inst.eventId,
                startDay = inst.startDay,
                durationDays = inst.durationDays,
                //isHidden = inst.isHidden
            });
        }

        File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true), Encoding.UTF8);

        tickIndex = 0;
        historyWriter = new StreamWriter(TickHistoryPath, append: true, Encoding.UTF8);

        Debug.Log($"[RunRecorder] Run started: {RunId}");
        Debug.Log($"[RunRecorder] Manifest: {ManifestPath}");
        Debug.Log($"[RunRecorder] TickHistory: {TickHistoryPath}");
    }

    public void RecordTick()
    {
        if (historyWriter == null)
        {
            Debug.LogError("[RunRecorder] historyWriter not opened. Call BeginRunAndWriteManifest() first");
            return;
        }
        if (marketSimulator == null)
        {
            Debug.LogError("[RunRecorder] marketSimulator is null.");
            return;
        }

        tickIndex++;

        var line = new TickLine
        {
            tick = tickIndex,
            day = gameStateController != null ? gameStateController.currentDay : 1,
            t = DateTime.UtcNow.ToString("o")
        };

        foreach (var kv in marketSimulator.GetAllStocks())
        {
            line.prices.Add(new TickPrice { id = kv.Key, p = kv.Value.currentPrice });
        }

        historyWriter.WriteLine(JsonUtility.ToJson(line, false));

        if (tickIndex % 10 == 0)
            historyWriter.Flush();
    }

    public void LogPaths()
    {
        Debug.Log($"[RunRecorder] persistentDataPath = {Application.persistentDataPath}");
        Debug.Log($"[RunRecorder] RunFolderPath = {RunFolderPath}");
        Debug.Log($"[RunRecorder] ManifestPath = {ManifestPath}");
        Debug.Log($"[RunRecorder] TickHistoryPath = {TickHistoryPath}");
    }


    public void OpenRunFolder()
    {
        if (!string.IsNullOrEmpty(RunFolderPath))
            Application.OpenURL("file://" + RunFolderPath);
    }
}