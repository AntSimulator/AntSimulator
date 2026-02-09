using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{

    public GameStateController gsc;
    public int currentSlotIndex = 0;

    public string GetSavePath(int slotIndex)
    {
        return Application.persistentDataPath + "/SaveSlot_" + slotIndex + ".json";
    }

    public void SaveToSlot(int slotIndex)
    {
        if (gsc == null) return;

        currentSlotIndex = slotIndex;

        SaveData data = new SaveData();
        data.day = gsc.currentDay;
        data.timer = gsc.stateTimer;
        data.stateName = gsc.currentStateName;
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
        string path = GetSavePath(slotIndex);

        if (!File.Exists(path))
        {
            Debug.Log("저장된 파일이 없습니다.");
            return;
        }
        currentSlotIndex = slotIndex;

        string json = File.ReadAllText(path);

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (gsc != null)
        {
            gsc.currentDay = data.day;
            gsc.LoadState(data.stateName, data.timer);

            if (gsc.calendarUI != null)
            {
                gsc.calendarUI.HighLightToday(gsc.currentDay);
            }
        }
    }
}
