using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class SaveSlotMenu : MonoBehaviour
{
    private SaveManager saveManager;
    public bool inSaveMode = true;


    [Header("UI ø¨∞·")]
    public GameObject slotWindow;
    public TextMeshProUGUI[] slotTexts;

    void Start()
    {
        if (saveManager == null)
        {
            saveManager = FindObjectOfType<SaveManager>();
        }

        CloseMenu();
    }

    public void OpenMenu()
    {
        if(slotWindow != null)
        {
            slotWindow.SetActive(true);
            RefreshUI();
        }
    }

    public void CloseMenu()
    {
        if(slotWindow != null)
        {
            slotWindow.SetActive(false);
        }
    }

    void OnEnable()
    {
        RefreshUI();
    }
    
    public void RefreshUI()
    {

        if (saveManager == null) return;

        for(int i = 0; i< slotTexts.Length; i++)
        {
            string path = saveManager.GetSavePath(i);

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                slotTexts[i].text = $"Slot {i + 1} | Day {data.day}\n<size=70%>{data.saveTime}</size>";
            }
            else
            {
                slotTexts[i].text = $"Slot {i + 1}\n(∫Û ΩΩ∑‘)";
            }
        }
    }

    public void OnSlotButtonClicked(int slotIndex)
    {
        if (saveManager == null) return;

        if (inSaveMode)
        {
            //saveManager.SaveToSlot(slotIndex);
            saveManager.StartNewGame(slotIndex);
            RefreshUI();
            Debug.Log($"ªı ∞‘¿” ΩΩ∑‘ {slotIndex}");
        }
        else
        {
            saveManager.LoadFromSlot(slotIndex);
            Debug.Log($"∫“∑Øø¿±‚ ΩΩ∑‘ {slotIndex}");
        }

    }

}
