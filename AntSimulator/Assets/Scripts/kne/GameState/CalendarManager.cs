using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CalendarManager : MonoBehaviour
{
    [Header("UI OBjects")]
    public GameObject calendarWindow;

    public TextMeshProUGUI openButtonText;
    public List<GameObject> dayCells = new List<GameObject>();

    public Color normalColor = Color.white;
    public Color todayColor = Color.yellow;

    private int _previousDay = -1;
    private GameStateController _gmc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        _gmc = Object.FindFirstObjectByType<GameStateController>();

        SetupNumbers();

        if(calendarWindow != null)
        {
            calendarWindow.SetActive(false);
        }

        UpdateDateOnButton();
        
    }

    void Update()
    {
        if (_gmc == null) return;

        if(_gmc.currentDay != _previousDay)
        {
            _previousDay = _gmc.currentDay;
            UpdateDateOnButton();
            HighLightToday(_gmc.currentDay);
        }
    }

    public void OpenCalendar()
    {
        if (calendarWindow != null)
        {
            calendarWindow.SetActive(true);
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

    public void CloseCalendar()
    {
        if (calendarWindow != null)
        {
            calendarWindow.SetActive(false);
        }
    }

    public void UpdateDateOnButton()
    {
        GameStateController gmc = Object.FindFirstObjectByType<GameStateController>();

        if (gmc != null && openButtonText != null)
        {
            openButtonText.text = gmc.currentDay.ToString();
        }
    
    }
}
