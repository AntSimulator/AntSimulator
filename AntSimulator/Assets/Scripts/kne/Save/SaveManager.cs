using UnityEngine.SceneManagement;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections;
using Player.Runtime;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public GameStateController gsc;
    public int currentSlotIndex = 0;
    private PlayerController _savePlayer;
    public MarketSimulator marketSimulator;
    [SerializeField] private List<StockDefinition> allStocks;

    public string GetSavePath(int slotIndex)
    {
        return Application.persistentDataPath + "/SaveSlot_" + slotIndex + ".json";
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveToSlot(int slotIndex)
    {
        currentSlotIndex = slotIndex;
        if (gsc == null) gsc = FindObjectOfType<GameStateController>();

        SaveData data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name;

        if (gsc != null)
        {
            data.day = gsc.currentDay;
            data.timer = gsc.stateTimer;
            data.stateName = gsc.currentStateName;

            if(_savePlayer == null)
            {
                _savePlayer = FindObjectOfType<PlayerController>();
            }

            if(_savePlayer != null)
            {
                data.saveCash = _savePlayer.GetCash();
                data.saveStocks.Clear();
                foreach (StockDefinition stock in allStocks)
                {
                    int qty = _savePlayer.GetQuantityByStockId(stock.stockId);

                    if (qty > 0)
                    {
                        data.saveStocks.Add(new StockSaveData
                        {
                            stockId = stock.stockId,
                            amount = qty
                        });
                    }
                }
            }

            if (marketSimulator == null)
            {
                marketSimulator = FindObjectOfType<MarketSimulator>();
            }

            if (marketSimulator != null)
            {
                data.marketPrices.Clear();

                foreach (var kvp in marketSimulator.GetAllStocks())
                {
                    data.marketPrices.Add(new StockPriceData
                    {
                        stockId = kvp.Key,
                        currentPrice = kvp.Value.currentPrice
                    });
                }
            }
        }
        else
        {
            Debug.Log("새 게임을 시작합니다...");
            data.day = 1;
            data.timer = 0f;
            data.stateName = "PreMarketState";

            SceneManager.LoadScene("IntroScene");
        }
        
        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string json = JsonUtility.ToJson(data);
        string path = GetSavePath(slotIndex);
        File.WriteAllText(path, json);

        Debug.Log($"[슬롯 {slotIndex}] 저장 완료");
    }

    public void AutoSave()
    {
        Debug.Log("자동 저장 중...");
        SaveToSlot(currentSlotIndex);
    }


    public void LoadFromSlot(int slotIndex)
    {

        StartCoroutine(LoadRoutine(slotIndex));
        
    }

    IEnumerator LoadRoutine(int slotIndex)
    {
        currentSlotIndex = slotIndex;
        string path = GetSavePath(slotIndex);

        if (!File.Exists(path))
        {
            Debug.Log("저장된 파일이 없습니다.");
            yield break;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut();

        string targetScene = string.IsNullOrEmpty(data.sceneName) ? "IntroScene" : data.sceneName;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        gsc = FindObjectOfType<GameStateController>();

        if (gsc != null)
        {
            gsc.currentDay = data.day;
            gsc.LoadState(data.stateName, data.timer);

            if (gsc.calendarUI != null)
            {
                gsc.calendarUI.HighLightToday(gsc.currentDay);
            }

            if (_savePlayer == null)
            {
                _savePlayer = FindObjectOfType<PlayerController>();
            }

            if (_savePlayer != null)
            {
                _savePlayer.SetSaveCash(data.saveCash);
                foreach (StockSaveData saveData in data.saveStocks)
                {
                    _savePlayer.SetQuantityByStockId(saveData.stockId, saveData.amount);
                }
            }

            if (marketSimulator == null)
            {
                marketSimulator = FindObjectOfType<MarketSimulator>();
            }

            if (marketSimulator != null)
            {
                var allStocks = marketSimulator.GetAllStocks();

                foreach (var savedStock in data.marketPrices)
                {
                    if (allStocks.TryGetValue(savedStock.stockId, out var stockState))
                    {
                        stockState.currentPrice = savedStock.currentPrice;
                        stockState.prevPrice = savedStock.currentPrice;
                    }
                }
            }

            Debug.Log($"로드 완료! {targetScene} 씬으로 이동했습니다.");
        }
        else
        {
            Debug.LogError("이동한 씬에 GameStateController가 없습니다!");
        }

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn();
    }

    public void ExitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

}
