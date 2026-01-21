using UnityEngine;

public class PreMarketState : IGameState
{
    private string gameStateName = "PreMarketState";
    private GameStateController gsc;
    private float timer = 0f;
    private float duration = 30f;

    public PreMarketState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
        timer = 0f;
    }

    public void Tick()
    {
        timer += Time.deltaTime;

        if(timer >= duration)
        {
            gsc.ChangeState(new MarketOpenState(gsc));
        }
    }

    public void Exit() { }
}
