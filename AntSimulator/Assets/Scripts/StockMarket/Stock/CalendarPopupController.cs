using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarPopupController : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TMP_Text lineText;       // 아래에 한 줄씩 나오는 텍스트
    public Image eventImage;        // 이벤트 이미지
    public Button nextButtonArea;   // 패널 전체 클릭용(투명 버튼 추천)
    public Button closeButton;      // optional

    [Header("Options")]
    public bool allowSkipTypingOnClick = true;

    private Coroutine _co;
    private Action _onClosed;

    private bool _clicked;
    private bool _forceFinishLine;

    void Awake()
    {
        if (nextButtonArea != null)
            nextButtonArea.onClick.AddListener(OnClickNext);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Show(EventPresentationSO pres, EventDefinition def, int day, int tick, Action onClosed)
    {
        _onClosed = onClosed;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        if (eventImage != null)
        {
            eventImage.sprite = pres != null ? pres.image : null;
            eventImage.gameObject.SetActive(eventImage.sprite != null);
        }

        if (lineText != null)
            lineText.text = "";

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(RunCalendarCo(pres));
    }

    IEnumerator RunCalendarCo(EventPresentationSO pres)
    {
        if (pres == null || pres.calendarLines == null || pres.calendarLines.Count == 0)
        {
            // 라인이 없으면 그냥 닫기
            yield return new WaitForSecondsRealtime(0.2f);
            Close();
            yield break;
        }

        for (int i = 0; i < pres.calendarLines.Count; i++)
        {
            string line = pres.calendarLines[i] ?? "";

            // 타이핑
            yield return TypeLineCo(line, Mathf.Max(1f, pres.charsPerSecond));

            // 다음 줄로 넘어가기 대기
            if (pres.autoAdvanceSeconds > 0f)
            {
                // 자동 + 클릭도 허용
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
                // 클릭 기다리기
                _clicked = false;
                while (!_clicked) yield return null;
            }
        }

        // 끝나면 닫기
        yield return new WaitForSecondsRealtime(0.15f);
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
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);

        var cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
    }
}