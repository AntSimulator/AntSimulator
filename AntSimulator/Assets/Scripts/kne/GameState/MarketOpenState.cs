using UnityEngine;

public class MarketOpenState : IGameState
{
    private string gameStateName = "MarketOpenState";
    private GameStateController gsc;
    private float duration = 150f;
    private int lastDisplayedSec = -1;

    public MarketOpenState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        gsc.currentStateName = gameStateName;
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
        lastDisplayedSec = -1;
    }

    public void Tick() {
        gsc.stateTimer += Time.deltaTime;
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
        Debug.Log("장이 마감되었습니다.");
    }
    
}
