using UnityEngine;

public class PreMarketState : IGameState
{
    private string gameStateName = "PreMarketState";
    private GameStateController gsc;
    private float duration = 10f;
    private int lastDisplayedSecond = -1;

    public PreMarketState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        gsc.currentStateName = gameStateName;
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
        lastDisplayedSecond = -1;
    }

    public void Tick()
    {
        gsc.stateTimer += Time.deltaTime;

        float remainTime = duration - gsc.stateTimer;
        int sec = Mathf.CeilToInt(remainTime);

        if(sec != lastDisplayedSecond)
        {
            if (gsc.stateInfoText != null)
            {
                gsc.stateInfoText.text = $"Open in <color=yellow>{sec:D2}</color>";
            }
            lastDisplayedSecond = sec;
        }

        if(gsc.stateTimer >= duration)
        {
            gsc.ChangeState(new MarketOpenState(gsc));
        }
    }

    public void Exit() { }
}
