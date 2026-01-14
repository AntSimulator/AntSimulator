using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateController : MonoBehaviour
{
    private IGameState currentState;
    public int currentDay = 1;
    public CalendarManager calendarUI;

    void Start()
    {
        ChangeState(new MarketOpenState(this));
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

        Debug.Log("날짜가 바뀌었습니다! 현재: " + currentDay + "일");
    }
}
