using UnityEngine;

public class PreMarketState : IGameState
{
    private string gameStateName = "PreMarketState";
    private GameStateController gsc;
    private float duration = 10f;//원래 10초 
    private int lastDisplayedSecond = -1;

    public PreMarketState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        gsc.currentStateName = gameStateName;
        Debug.Log($"[GameStateNName] ���� {gameStateName}���� �Դϴ�.");
        if (gsc.market != null)
        {
            gsc.market.simulateTicks = false;
            gsc.market.allowEventReveal = true;
            gsc.market.ProcessEventRevealNow(gsc.currentDay, 0);
        }
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
