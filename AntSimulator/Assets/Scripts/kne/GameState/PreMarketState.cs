using UnityEngine;

public class PreMarketState : IGameState
{
    private string gameStateName = "PreMarketState";
    private GameStateController gsc;
    private float timer = 0f;
    private float duration = 10f;
    private int lastDisplayedSecond = -1;

    public PreMarketState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
        timer = 0f;
        lastDisplayedSecond = -1;
    }

    public void Tick()
    {
        timer += Time.deltaTime;

        float remainTime = duration - timer;
        int sec = Mathf.CeilToInt(remainTime);

        if(sec != lastDisplayedSecond)
        {
            if (gsc.stateInfoText.text != null)
            {
                gsc.stateInfoText.text = $"Open in <color=yellow>{sec:D2}</color>";
            }
            lastDisplayedSecond = sec;
        }

        if(timer >= duration)
        {
            gsc.ChangeState(new MarketOpenState(gsc));
        }
    }

    public void Exit() { }
}
