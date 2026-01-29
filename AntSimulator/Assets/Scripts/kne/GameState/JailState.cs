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
        Debug.Log($"[GameStateNName] ���� {gameStateName}���� �Դϴ�.");
    }

    public void Tick() { }
    public void Exit() { }

}
