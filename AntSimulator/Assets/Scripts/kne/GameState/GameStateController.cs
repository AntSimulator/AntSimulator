using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateController : MonoBehaviour
{
    private IGameState currentState;

    void Start()
    {
        ChangeState(new MarketOpenState());
        
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.Tick();

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame) ChangeState(new MarketOpenState());
        if (keyboard.digit2Key.wasPressedThisFrame) ChangeState(new JailState());
        if (keyboard.digit3Key.wasPressedThisFrame) ChangeState(new SettlementState());
        
    }

    public void ChangeState(IGameState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
}
