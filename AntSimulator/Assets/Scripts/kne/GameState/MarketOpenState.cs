using UnityEngine;

public class MarketOpenState : IGameState
{
    private string gameStateName = "MarketOpenState";
    private GameStateController gsc;
    private float timer = 0f;
    private float duration = 300f;

    public MarketOpenState(GameStateController gsc)
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


        if(timer >= duration)
        {
            gsc.ChangeState(new SettlementState(gsc));
        }
    
    }

    public void Exit() {
        Debug.Log("장이 마감되었습니다.");
    }
    
}
