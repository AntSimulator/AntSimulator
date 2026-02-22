using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.UI;

public class CalendarPopupController : MonoBehaviour
{
    [Header("Refs")]
    public PopupManager popupManager;

    [Header("Popup Id")]
    public string popupId = "Popup_Calendar"; // PopupManager에 등록한 id

    [Header("UI")]
    public TMP_Text lineText;       // 아래에 한 줄씩 나오는 텍스트
    public Image eventImage;        // 이벤트 이미지
    public Button nextButtonArea;   // 패널 전체 클릭용(투명 버튼 추천)
    public Button closeButton;      // optional

    [Header("Behavior")]
    public bool pauseGameWhileOpen = true;
    public bool allowSkipTypingOnClick = true;
    
    [Header("WED Finance BGM")]
    public bool enableWedFinanceBgm = true;
    public string wedGoodEventId;
    public string wedBadEventId;
    public AudioSource bgmSource;                  
    public AudioClip wedGoodBgm;
    public AudioClip wedBadBgm;


    private Coroutine _co;
    private Action _onClosed;

    private bool _isOpen;
    private bool _clicked;
    private bool _forceFinishLine;

    private float _prevTimeScale = 1f;

    void Awake()
    {
        if (nextButtonArea != null)
            nextButtonArea.onClick.AddListener(OnClickNext);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // 시작 시 닫힘 상태로 맞추기(안전)
        if (popupManager != null)
            popupManager.Close(popupId);

        _isOpen = false;
    }

    public void Show(EventPresentationSO pres, EventDefinition def, int day, int tick, Action onClosed)
    {
        _onClosed = onClosed;
        TryPlayWedFinanceBgm(def, day);
        // 1) 팝업 열기
        if (popupManager != null)
            popupManager.Open(popupId);

        _isOpen = true;

        // 2) 게임 멈추기
        if (pauseGameWhileOpen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // 3) 이미지/텍스트 세팅
        if (eventImage != null)
        {
            eventImage.sprite = pres != null ? pres.image : null;
            eventImage.gameObject.SetActive(eventImage.sprite != null);
        }

        if (lineText != null)
            lineText.text = "";

        // 4) 코루틴 시작
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(RunCalendarCo(pres));
    }

    IEnumerator RunCalendarCo(EventPresentationSO pres)
    {
        if (pres == null || pres.calendarLines == null || pres.calendarLines.Count == 0)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            Close();
            yield break;
        }

        for (int i = 0; i < pres.calendarLines.Count; i++)
        {
            string line = pres.calendarLines[i] ?? "";

            // 타이핑
            yield return TypeLineCo(line, Mathf.Max(1f, pres.charsPerSecond));

            // 다음 줄 대기
            if (pres.autoAdvanceSeconds > 0f)
            {
                _clicked = false;
                float t = 0f;
                while (t < pres.autoAdvanceSeconds && !_clicked)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                _clicked = false;
                while (!_clicked) yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(0.1f);
        Close();
    }

    IEnumerator TypeLineCo(string line, float cps)
    {
        _forceFinishLine = false;
        _clicked = false;

        if (lineText == null) yield break;

        lineText.text = "";

        int total = line.Length;
        float acc = 0f;
        int shown = 0;

        while (shown < total)
        {
            if (_forceFinishLine)
            {
                lineText.text = line;
                yield break;
            }

            acc += Time.unscaledDeltaTime * cps;
            int add = Mathf.FloorToInt(acc);
            if (add > 0)
            {
                acc -= add;
                shown = Mathf.Min(total, shown + add);
                lineText.text = line.Substring(0, shown);
            }

            // 클릭하면 타이핑 스킵
            if (allowSkipTypingOnClick && _clicked)
            {
                _clicked = false;
                _forceFinishLine = true;
            }

            yield return null;
        }
    }

    void OnClickNext()
    {
        // 타이핑 중이면 스킵, 타이핑 끝났으면 다음 줄로
        _clicked = true;
    }

    public void Close()
    {
        if (!_isOpen) return; // ✅ 중복 Close 방지

        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
        
        if(bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();

        // 팝업 닫기
        if (popupManager != null)
            popupManager.Close(popupId);

        _isOpen = false;

        // 타임스케일 복구
        if (pauseGameWhileOpen && Time.timeScale == 0f)
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;

        // ✅ 라우터 큐 진행
        var cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
    }

    // 버튼에서 직접 연결하고 싶으면 (옵션)
    public void OnClickCloseButton()
    {
        Close();
    }

    void TryPlayWedFinanceBgm(EventDefinition def, int day)
    {
        if (!enableWedFinanceBgm) return;
        if (def == null) return;
        if (day != 2) return;
        if(bgmSource == null) return;

        // good/bad는 eventId로 판별
        if (!string.IsNullOrEmpty(wedGoodEventId) && def.eventId == wedGoodEventId && wedGoodBgm != null)
        {
            bgmSource.clip = wedGoodBgm;
            bgmSource.loop = false;
            bgmSource.Play();
        }
        else if (!string.IsNullOrEmpty(wedBadEventId) && def.eventId == wedBadEventId && wedBadBgm != null)
        {
            bgmSource.clip = wedBadBgm;
            bgmSource.loop = false;
            bgmSource.Play();
        }
    }
}