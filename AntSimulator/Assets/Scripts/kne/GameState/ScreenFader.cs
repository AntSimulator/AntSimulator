using UnityEngine;
using UnityEngine.UI;
using System.Collections;

//하루 넘어갈 때 페이드인-페이드아웃도구 프리팹입니다

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    public Image blackScreen;
    public float fadeDuration = 1.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (blackScreen != null) blackScreen.gameObject.SetActive(false);
    }

    public IEnumerator FadeOut()
    {
        if (blackScreen == null) yield break;

        blackScreen.gameObject.SetActive(true);
        Color c = blackScreen.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = timer / fadeDuration;
            blackScreen.color = c;
            yield return null;
        }
        blackScreen.color = new Color(c.r, c.g, c.b, 1);
    }

    public IEnumerator FadeIn()
    {
        if (blackScreen == null) yield break;

        Color c = blackScreen.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = 1f - (timer / fadeDuration);
            blackScreen.color = c;
            yield return null;
        }
        blackScreen.gameObject.SetActive(false);
    }
}
