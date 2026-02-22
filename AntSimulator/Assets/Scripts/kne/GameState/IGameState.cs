using UnityEngine;

public interface IGameState
{
    void Enter();

    void Tick();
    void Exit();
    

}
