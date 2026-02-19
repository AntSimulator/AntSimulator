using System.Collections;
using TMPro;
using UnityEngine;

public class NewsUIController : MonoBehaviour
{
    [Header("Refs")]
    public MarketSimulator market;

    [Header("UI")]
    [Tooltip("뉴스 팝업 전체 루트(패널). SetActive로 켜고 끔")]
    public GameObject popupRoot;

    [Tooltip("본문 텍스트(이벤트 description)")]
    public TMP_Text contentText;

    [Header("Behavior")]
    [Tooltip("팝업이 떠있는 동안 게임 멈추기")]
    public bool pauseGameWhileOpen = true;

    [Tooltip("자동으로 닫히는 시간(초)")]
    public float autoCloseSeconds = 3f;

    [Tooltip("클릭하면 즉시 닫기(루트에 버튼/투명 클릭 영역 붙이면 좋음)")]
    public bool clickToClose = true;

    Coroutine _closeRoutine;
    float _prevTimeScale = 1f;

    void Awake()
    {
        // 시작 시 숨김
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (market != null)
            market.OnEventRevealed += HandleEventRevealed;
    }

    void OnDisable()
    {
        if (market != null)
            market.OnEventRevealed -= HandleEventRevealed;

        // 혹시 비활성화되며 멈춘 채로 남을 수 있어서 복구
        RestoreTimeScaleIfNeeded();
    }

    void Update()
    {
        if (!clickToClose) return;
        if (popupRoot == null || !popupRoot.activeSelf) return;

        // 마우스 클릭 또는 터치로 닫기
        if (Input.GetMouseButtonDown(0))
            Close();
    }

    void HandleEventRevealed(EventDefinition def, int day, int tick)
    {
        if (def == null) return;

        Open(def.description ?? "");
    }

    public void Open(string text)
    {
        if (popupRoot == null || contentText == null) return;

        // 내용 갱신
        contentText.text = text;

        // 팝업 표시
        popupRoot.SetActive(true);

        // 게임 멈추기 (Time.timeScale=0이면 WaitForSeconds가 안 돌아서 Realtime로 닫아야 함)
        if (pauseGameWhileOpen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // 기존 자동닫기 코루틴 있으면 취소 후 재시작
        if (_closeRoutine != null) StopCoroutine(_closeRoutine);
        _closeRoutine = StartCoroutine(AutoCloseRoutine());
    }

    IEnumerator AutoCloseRoutine()
    {
        // TimeScale=0이어도 동작하도록 real time 사용
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

        if (popupRoot != null)
            popupRoot.SetActive(false);

        RestoreTimeScaleIfNeeded();
    }

    void RestoreTimeScaleIfNeeded()
    {
        if (pauseGameWhileOpen && Time.timeScale == 0f)
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
    }
}