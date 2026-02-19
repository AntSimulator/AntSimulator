using UnityEngine;

public class SettlementState : IGameState
{
    private string gameStateName = "SettlementState";
    private GameStateController gsc;
    private float duration = 20f;
    private int lastDisplayedSec = -1;
    private bool isFinished = false;

    public SettlementState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        gsc.currentStateName = gameStateName;
        if (gsc.market != null)
        {
            gsc.market.simulateTicks = false;
            gsc.market.allowEventReveal = false;
        }
        isFinished = false;
        Debug.Log($"[GameStateNName] ���� {gameStateName}���� �Դϴ�.");
    }

    public void Tick() {
        if (isFinished) return;

        gsc.stateTimer += Time.deltaTime;
        float remainTime = duration - gsc.stateTimer;
        int sec = Mathf.CeilToInt(remainTime);
        
        if(sec != lastDisplayedSec)
        {
            if (gsc.stateInfoText != null)
            {
                gsc.stateInfoText.text = $"Until next day <color=yellow>{sec:D2}</color>";
            }

            lastDisplayedSec = sec;
        }
        
        if (gsc.stateTimer >= duration) {
            isFinished = true;
            gsc.StartDayTransition();
        }
    }

    public void Exit() {  }
    
}
