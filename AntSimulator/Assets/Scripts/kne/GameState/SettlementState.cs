using UnityEngine;

public class SettlementState : IGameState
{
    private string gameStateName = "SettlementState";
    private GameStateController gsc;

    public SettlementState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
    }

    public void Tick() { }
    public void Exit() {
        if (gsc != null) {
            gsc.NextDay();
        }
    }
    
}
