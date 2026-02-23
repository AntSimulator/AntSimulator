using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GlobalExitManager : MonoBehaviour
{
    public static GlobalExitManager Instance;

    [Header("종료 확인 창 패널")]
    public GameObject exitWindow;

    [Header("타이틀로 돌아가기 버튼")]
    public GameObject goTitleButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (exitWindow != null)
        {
            exitWindow.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (exitWindow != null)
            {
                bool isWindowActive = exitWindow.activeSelf;
                if (!isWindowActive)
                {
                    if (SceneManager.GetActiveScene().name == "TitleScene")
                    {
                        if (goTitleButton != null) goTitleButton.SetActive(false); 
                    }
                    else
                    {
                        if (goTitleButton != null) goTitleButton.SetActive(true);  
                    }
                }

                exitWindow.SetActive(!isWindowActive);

                // Time.timeScale = isWindowActive ? 1f : 0f; 
            }
        }
    }

    public void ConfirmExit()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    public void CancelExit()
    {
        if (exitWindow != null)
        {
            exitWindow.SetActive(false);
            // Time.timeScale = 1f; // 일시정지 해제
        }
    }

    public void GoToTitle()
    {
        //Time.timeScale = 1f;

        if (exitWindow != null)
        {
            exitWindow.SetActive(false);
        }

        SceneManager.LoadScene("TitleScene");
    }
}