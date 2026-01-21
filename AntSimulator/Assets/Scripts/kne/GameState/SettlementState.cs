using UnityEngine;

public class SettlementState : IGameState
{
    private string gameStateName = "SettlementState";
    private GameStateController gsc;
    private float timer = 0f;
    private float duration = 30f;

    public SettlementState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
        timer = 0f;
    }

    public void Tick() {
        timer += Time.deltaTime;

        if (timer >= duration) {
            gsc.ChangeState(new PreMarketState(gsc));
        }
    }

    public void Exit() {
        if (gsc != null) 
        {
            gsc.NextDay();
        }
    }
    
}
