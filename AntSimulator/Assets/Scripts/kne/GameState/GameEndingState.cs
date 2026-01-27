using UnityEngine;

public class GameEndingState : IGameState
{
    private GameStateController gsc;

    public GameEndingState(GameStateController gsc)
    {
        this.gsc = gsc;
    }

    public void Enter()
    {
        Debug.Log("Game end");

    }

    public void Tick() { }
    public void Exit() { }
    
}
