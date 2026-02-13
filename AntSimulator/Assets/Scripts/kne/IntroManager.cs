using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [System.Serializable]
    public class CutscenePage
    {
        public Sprite image;
        [TextArea(3, 5)]
        public string dialogue;
    }

    public List<CutscenePage> pages;

    [Header("설정")]
    public string nextSceneName = "";
    public float fadeSpeed = 2.0f; 

    [Header("UI 연결")]
    public Image displayImage;
    public TMP_Text displayText;

    private int currentIndex = 0;
    private bool isFading = false; 

    void Start()
    {
        SetPageData(currentIndex);
    }

    public void Next()
    {
        if (isFading) return;

        currentIndex++;

        if (currentIndex < pages.Count)
        {
            StartCoroutine(FadeTransition());
        }
        else
        {
            //SceneManager.LoadScene(nextSceneName);
        }
    }

    void SetPageData(int index)
    {
        if (pages.Count == 0) return;
        displayImage.sprite = pages[index].image;
        displayText.text = pages[index].dialogue;
    }

    IEnumerator FadeTransition()
    {
        isFading = true;

        float alpha = 1.0f;
        while (alpha > 0.0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            SetImageAlpha(alpha);
            yield return null;
        }

        SetImageAlpha(0.0f);
        SetPageData(currentIndex);

        while (alpha < 1.0f)
        {
            alpha += Time.deltaTime * fadeSpeed;
            SetImageAlpha(alpha);
            yield return null;
        }

        SetImageAlpha(1.0f); 
        isFading = false;
    }

    void SetImageAlpha(float alpha)
    {
        if (displayImage != null)
        {
            Color c = displayImage.color;
            c.a = alpha;
            displayImage.color = c;
        }

        if (displayText != null)
        {
            Color c = displayText.color;
            c.a = alpha;
            displayText.color = c;
        }
    }

    public void SkipIntro()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}