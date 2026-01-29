using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameStateController : MonoBehaviour
{
    private IGameState currentState;
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
        currentState?.Enter();
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
}
