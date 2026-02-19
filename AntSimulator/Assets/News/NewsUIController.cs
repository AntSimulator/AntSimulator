using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class NewsUIController : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("뉴스 팝업 전체 루트(패널). SetActive로 켜고 끔")]
    public GameObject popupRoot;

    [Tooltip("본문 텍스트(이벤트 description)")]
    public TMP_Text contentText;

    [Header("Behavior")]
    public float autoCloseSeconds = 3f;
    public bool clickToClose = true;

    private Coroutine _closeRoutine;
    private Action _onClosed;

    void Awake()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    void OnDisable()
    {
        // 비활성화되면 콜백은 날려버림(중복 호출 방지)
        _onClosed = null;

        if (_closeRoutine != null)
        {
            StopCoroutine(_closeRoutine);
            _closeRoutine = null;
        }
    }

    void Update()
    {
        if (!clickToClose) return;
        if (popupRoot == null || !popupRoot.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
            Close();
    }

    // ✅ Router가 호출할 함수
    public void Show(EventPresentationSO pres, EventDefinition def, int day, int tick, Action onClosed)
    {
        if (popupRoot == null || contentText == null) return;

        _onClosed = onClosed;

        // description만 표시
        contentText.text = def != null ? (def.description ?? "") : "";

        popupRoot.SetActive(true);

        if (_closeRoutine != null) StopCoroutine(_closeRoutine);
        _closeRoutine = StartCoroutine(AutoCloseRoutine());
    }

    IEnumerator AutoCloseRoutine()
    {
        float t = 0f;
        while (t < autoCloseSeconds)
        {
            yield return null;
            t += Time.unscaledDeltaTime; // ✅ timeScale=0이어도 흘러감
        }

        Close();
    }

    // 버튼에서 직접 연결할 수도 있게 public
    public void Close()
    {
        if (_closeRoutine != null)
        {
            StopCoroutine(_closeRoutine);
            _closeRoutine = null;
        }

        if (popupRoot != null)
            popupRoot.SetActive(false);

        var cb = _onClosed;
        _onClosed = null;
        cb?.Invoke(); // ✅ Router의 ShowNext 호출
    }
}