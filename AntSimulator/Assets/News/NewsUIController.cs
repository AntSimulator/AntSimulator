using System.Collections;
using TMPro;
using UnityEngine;
using Utils.UI;
using System;
using UnityEngine.InputSystem;

public class NewsUIController : MonoBehaviour
{
    [Header("Refs")]
    public MarketSimulator market;
    public PopupManager popupManager;

    [Header("Popup Id")]
    public string popupId = "Popup_News"; // PopupManager에 등록한 id

    [Header("UI (Only content)")]
    public TMP_Text contentText;

    [Header("Behavior")]
    public bool pauseGameWhileOpen = true;
    public float autoCloseSeconds = 3f;
    public bool clickToClose = true;

    Coroutine _closeRoutine;
    float _prevTimeScale = 1f;
    bool _isOpen;
    
    private Action _onClosed;

    void OnEnable()
    {
        if (market != null)
            market.OnEventRevealed += HandleEventRevealed;
    }

    void OnDisable()
    {
        if (market != null)
            market.OnEventRevealed -= HandleEventRevealed;

        RestoreTimeScaleIfNeeded();
    }

    void Update()
    {
        
        if (!clickToClose) return;
        if (!_isOpen) return; // ✅ popupRoot 대신 이걸로 체크

        // 마우스
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Close();

        // 터치(모바일)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            Close();
    }

    void HandleEventRevealed(EventDefinition def, int day, int tick)
    {
        if (def == null) return;
        Open(def.description ?? "");
    }

    public void Open(string text)
    {
        if (contentText == null) return;

        contentText.text = text;

        // 팝업 열기
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
    }

    void RestoreTimeScaleIfNeeded()
    {
        if (pauseGameWhileOpen && Time.timeScale == 0f)
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
    }
    

    public void Show(EventPresentationSO pres, EventDefinition def, int day, int tick, Action onClosed)
    {
        _onClosed = onClosed;
        Open(def != null ? (def.description ?? "") : "");
    }
}