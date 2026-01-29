using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CalendarManager : MonoBehaviour
{

    public List<GameObject> dayCells = new List<GameObject>();

    public Color normalColor = Color.white;
    public Color todayColor = Color.yellow;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupNumbers();

        GameStateController gmc = Object.FindFirstObjectByType<GameStateController>();
        if(gmc != null)
        {
            HighLightToday(gmc.currentDay);
        }

    }

    public void SetupNumbers()
    {
        for (int i = 0; i < dayCells.Count; i++) {
            var text = dayCells[i].GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = (i + 1).ToString();
        }
    }

    // Update is called once per frame
    public void HighLightToday(int currentDay)
    {
        for (int i = 0; i < dayCells.Count; i++) {
            dayCells[i].GetComponent<Image>().color = (i == currentDay - 1) ? todayColor : normalColor;
        }
    }
}
