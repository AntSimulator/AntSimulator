using UnityEngine;

public class TitleManager : MonoBehaviour
{
    public void ClickExit()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}
