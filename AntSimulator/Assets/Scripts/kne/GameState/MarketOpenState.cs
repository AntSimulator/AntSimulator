using UnityEngine;

public class MarketOpenState : IGameState
{
    private string gameStateName = "MarketOpenState";

    public void Enter()
    {
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
    }
    public void Tick() { }
    public void Exit() { }
    
}
