using UnityEngine;

public class MarketOpenState : IGameState
{
    private string gameStateName = "MarketOpenState";
    private GameStateController gsc;
    private float duration = 150f;//원래 150
    private int lastDisplayedSec = -1;
    private float tickTimer;

    public MarketOpenState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        gsc.currentStateName = gameStateName;
        Debug.Log($"[GameStateNName] ���� {gameStateName}���� �Դϴ�.");
        if (gsc.market != null)
        {
            gsc.market.allowEventReveal = true;
            gsc.market.simulateTicks = true;
        }
        lastDisplayedSec = -1;
    }

    public void Tick() {
        gsc.stateTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;
        if (tickTimer >= gsc.market.tickIntervalSec)
        {
            tickTimer -= gsc.market.tickIntervalSec;
            gsc.market.TickOnce();
        }
        float remainTime = duration - gsc.stateTimer;
        int totalSeconds = Mathf.FloorToInt(remainTime);

        if (totalSeconds != lastDisplayedSec)
        {

            int min = totalSeconds / 60;
            int sec = totalSeconds % 60;
            if (gsc.stateInfoText != null)
            {
                gsc.stateInfoText.text = $"Close in <color=green>{min:D2}:{sec:D2}</color>";
            }

            lastDisplayedSec = totalSeconds;
        }
        

        if(gsc.stateTimer >= duration)
        {
            gsc.ChangeState(new SettlementState(gsc));
        }
    
    }

    public void Exit() {
        if (gsc.market != null)
        {
            gsc.market.simulateTicks = false;
        }
        Debug.Log("���� �����Ǿ����ϴ�.");
    }
    
}
