using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TitleSequence : MonoBehaviour
{
    [Header("페이드 인 설정")]
    public Image fadePanel;
    public float screenFadeDuration = 2.0f;

    [Header("로고 이동 설정")]
    public RectTransform logoRect;
    public Vector2 logoEndPosition = Vector2.zero;
    public float logoMoveDuration = 1.5f; 
    private Vector2 logoStartPosition; 

    [Header("버튼 등장 설정")]
    public CanvasGroup[] buttons; 
    public float buttonFadeDuration = 0.8f;
    public float delayBetweenButtons = 0.3f;

    void Start()
    {
        fadePanel.color = new Color(0, 0, 0, 1);
        logoStartPosition = logoRect.anchoredPosition;
        foreach (var btn in buttons)
        {
            btn.alpha = 0f;
            btn.interactable = false;
            btn.blocksRaycasts = false;
        }

        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        float timer = 0f;
        Color startColor = fadePanel.color;
        Color endColor = new Color(0, 0, 0, 0);

        while (timer < screenFadeDuration)
        {
            timer += Time.deltaTime;
            fadePanel.color = Color.Lerp(startColor, endColor, timer / screenFadeDuration);
            yield return null;
        }
        fadePanel.gameObject.SetActive(false);

        timer = 0f;
        while (timer < logoMoveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / logoMoveDuration);
            logoRect.anchoredPosition = Vector2.Lerp(logoStartPosition, logoEndPosition, t);
            yield return null;
        }

        foreach (var btn in buttons)
        {
            StartCoroutine(FadeInButton(btn));
            yield return new WaitForSeconds(delayBetweenButtons);
        }
    }

    IEnumerator FadeInButton(CanvasGroup cg)
    {
        float timer = 0f;
        while (timer < buttonFadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, timer / buttonFadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }
}