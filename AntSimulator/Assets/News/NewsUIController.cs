using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Utils.UI;

public class NewsUIController : MonoBehaviour
{
    [Header("Refs")]
    public PopupManager popupManager;

    [Header("Popup Id")]
    public string popupId = "Popup_News";

    [Header("UI (Only content)")]
    public TMP_Text contentText;

    [Header("Behavior")]
    public bool pauseGameWhileOpen = true;
    public float autoCloseSeconds = 3f;
    
    [Header("Audio")]
    public AudioSource bgmSource;
    public AudioClip blep;

    Coroutine _closeRoutine;
    float _prevTimeScale = 1f;
    bool _isOpen;

    private Action _onClosed;

    public void Show(EventPresentationSO pres, EventDefinition def, int day, int tick, Action onClosed)
    {
        _onClosed = onClosed;
        Open(def != null ? (def.description ?? "") : "");
    }

    public void Open(string text)
    {
        if(bgmSource == null) return;
        if(blep == null) return;

        bgmSource.clip = blep;
        bgmSource.loop = false;
        bgmSource.Play();
        
        if (contentText == null) return;

        contentText.text = text;

        if (popupManager != null)
            popupManager.Open(popupId);

        _isOpen = true;

        if (pauseGameWhileOpen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (_closeRoutine != null) StopCoroutine(_closeRoutine);
        _closeRoutine = StartCoroutine(AutoCloseRoutine());
    }

    IEnumerator AutoCloseRoutine()
    {
        float t = 0f;
        while (t < autoCloseSeconds)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
        }
        Close();
    }

    public void OnClickCloseButton() => Close();

    public void Close()
    {
        if (_closeRoutine != null)
        {
            StopCoroutine(_closeRoutine);
            _closeRoutine = null;
        }

        if (popupManager != null)
            popupManager.Close(popupId);

        _isOpen = false;
        RestoreTimeScaleIfNeeded();

        var cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();   // ✅ 딱 1번만 호출
    }

    void RestoreTimeScaleIfNeeded()
    {
        if (pauseGameWhileOpen && Time.timeScale == 0f)
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
    }
}