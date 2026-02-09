using UnityEngine;

public class SettlementState : IGameState
{
    private string gameStateName = "SettlementState";
    private GameStateController gsc;
    private float duration = 20f;
    private int lastDisplayedSec = -1;

    public SettlementState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        gsc.currentStateName = gameStateName;
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
    }

    public void Tick() {
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
            gsc.NextDay();
            if(gsc.currentDay > gsc.targetDay)
            {
                gsc.ChangeState(new GameEndingState(gsc));
            }
            else
            {
                gsc.ChangeState(new PreMarketState(gsc));
            }
        }
    }

    public void Exit() {  }
    
}
