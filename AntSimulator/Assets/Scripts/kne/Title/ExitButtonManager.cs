using UnityEngine;

public class ExitButtonManager : MonoBehaviour
{
    public void ExitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

}
