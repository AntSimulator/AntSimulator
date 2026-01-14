using UnityEngine;

public class JailState : IGameState
{
    private string gameStateName = "JailState";
    private GameStateController gsc;

    public JailState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        Debug.Log($"[GameStateNName] 현재 {gameStateName}상태 입니다.");
    }

    public void Tick() { }
    public void Exit() { }
    
}
