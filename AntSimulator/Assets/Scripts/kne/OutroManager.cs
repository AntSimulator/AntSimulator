using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class OutroManager : MonoBehaviour
{
    [System.Serializable]
    public class CutscenePage
    {
        public Sprite image;
        [TextArea(3, 5)]
        public string dialogue;
    }

    public List<CutscenePage> pages;

    [Header("����")]
    public string nextSceneName = "TitleScene";
    public float fadeSpeed = 2.0f;

    [Header("UI ����")]
    public Image displayImage;
    public TMP_Text displayText;
    
    [Header("sound")] 
    public AudioSource bgmSource;
    public AudioClip EndingSound;
  

    private int currentIndex = 0;
    private bool isFading = false;

    void Start()
    {
        SetPageData(currentIndex);
        PlayKeybordSound();
        if (ScreenFader.Instance != null)
        {
            StartCoroutine(ScreenFader.Instance.FadeIn());
        }
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
            SceneManager.LoadScene(nextSceneName);
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
    void PlayKeybordSound()
    {
        Debug.Log($"[Outro] bgmSource={(bgmSource ? bgmSource.name : "NULL")}, clip={(EndingSound ? EndingSound.name : "NULL")}, listenerPause={AudioListener.pause}, timeScale={Time.timeScale}");
        bgmSource.clip = EndingSound;
        bgmSource.loop = false;
        bgmSource.Play();
        Debug.Log($"[Outro] isPlaying={bgmSource.isPlaying}, volume={bgmSource.volume}, mute={bgmSource.mute}, spatialBlend={bgmSource.spatialBlend}");
    }
}
