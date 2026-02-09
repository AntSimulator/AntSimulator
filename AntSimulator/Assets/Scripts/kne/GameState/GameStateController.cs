using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameStateController : MonoBehaviour
{
    public SaveManager saveManager;
    private bool isRestoring = false;

    private IGameState currentState;
    public string currentStateName = "";
    public float stateTimer = 0f;
    public int currentDay = 1;
    public CalendarManager calendarUI;
    public TextMeshProUGUI stateInfoText;

    public System.Action<int> OnDayStarted;

    [Header("Ending Settings")]
    public int targetDay = 4;


    void Start()
    {
        Application.targetFrameRate = 30;
        ChangeState(new PreMarketState(this));
        calendarUI.HighLightToday(currentDay);

    }

    // Update is called once per frame
    void Update()
    {
        currentState?.Tick();

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame) ChangeState(new MarketOpenState(this));
        if (keyboard.digit2Key.wasPressedThisFrame) ChangeState(new JailState(this));
        if (keyboard.digit3Key.wasPressedThisFrame) ChangeState(new SettlementState(this));

    }

    public void ChangeState(IGameState newState)
    {
        currentState?.Exit();
        currentState = newState;
        stateTimer = 0f;
        currentState?.Enter();

        if(saveManager != null && isRestoring == false)
        {
            saveManager.AutoSave();
            Debug.Log("자동 저장되었습니다.");
        } 
    }

    public void NextDay()
    {
        currentDay++;

        if (calendarUI != null)
        {
            calendarUI.HighLightToday(currentDay);
        }
        
        OnDayStarted?.Invoke(currentDay);

        Debug.Log("?????? ????????????! ????: " + currentDay + "??");
    }

    public void LoadState(string stateName, float savedTime)
    {
        isRestoring = true;
        

        if (stateName == "PreMarketState")
        {
            ChangeState(new PreMarketState(this));
        }
        else if (stateName == "MarketOpenState")
        {
            ChangeState(new MarketOpenState(this));
        }else if (stateName == "SettlementState")
        {
            ChangeState(new SettlementState(this));
        }else if (stateName == "JailState")
        {
            ChangeState(new JailState(this));
        }

        stateTimer = savedTime;
        currentStateName = stateName;

        isRestoring = false;
    }
}
