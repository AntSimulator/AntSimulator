using System;
using System.Collections.Generic;
using UnityEngine;

public class EventPopupRouter : MonoBehaviour
{
    [Header("Refs")]
    public MarketSimulator market;
    public EventPresentationDatabaseSO presentationDb;

    [Header("Popups")]
    public NewsUIController newsPopup;          // ✅ NewsPopupController 대신 이거 사용
    public CalendarPopupController calendarPopup;

    [Header("Pause")]
    public bool pauseGameWhilePopup = true;

    private readonly Queue<(EventDefinition def, int day, int tick)> _queue = new();
    private bool _isShowing;
    private float _prevTimeScale = 1f;

    void OnEnable()
    {
        if (market != null)
            market.OnEventRevealed += HandleEventRevealed;
    }

    void OnDisable()
    {
        if (market != null)
            market.OnEventRevealed -= HandleEventRevealed;

        // 혹시 남아있으면 복구
        ResumeIfNeeded(force: true);
    }

    void HandleEventRevealed(EventDefinition def, int day, int tick)
    {
        if (def == null) return;

        _queue.Enqueue((def, day, tick));

        if (!_isShowing)
            ShowNext();
    }

    void ShowNext()
    {
        if (_queue.Count == 0)
        {
            _isShowing = false;
            ResumeIfNeeded();
            return;
        }

        _isShowing = true;

        var (def, day, tick) = _queue.Dequeue();

        if (presentationDb == null)
        {
            Debug.LogError("[EventPopupRouter] presentationDb is null");
            _isShowing = false;
            ResumeIfNeeded();
            return;
        }

        var pres = presentationDb.Find(def.eventId);

        Debug.Log($"[Router] eventId={def.eventId} kind={def.kind} pres={(pres ? pres.presentationType.ToString() : "NULL")}");

        PauseIfNeeded();

        // pres 없으면 기본은 News
        if (pres == null)
        {
            if (newsPopup == null)
            {
                Debug.LogError("[EventPopupRouter] newsPopup is null");
                ShowNext();
                return;
            }
            newsPopup.Show(null, def, day, tick, ShowNext);
            return;
        }

        switch (def.kind)
        {
            case EventKind.Calendar:
                if (calendarPopup == null)
                {
                    Debug.LogError("[EventPopupRouter] calendarPopup is null");
                    ShowNext();
                    return;
                }
                calendarPopup.Show(pres, def, day, tick, ShowNext);
                break;

            default:
                if (newsPopup == null)
                {
                    Debug.LogError("[EventPopupRouter] newsPopup is null");
                    ShowNext();
                    return;
                }
                newsPopup.Show(pres, def, day, tick, ShowNext);
                break;
        }
    }

    void PauseIfNeeded()
    {
        if (!pauseGameWhilePopup) return;

        if (Time.timeScale > 0f)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    void ResumeIfNeeded(bool force = false)
    {
        if (!pauseGameWhilePopup) return;

        if (force || Time.timeScale == 0f)
            Time.timeScale = Mathf.Approximately(_prevTimeScale, 0f) ? 1f : _prevTimeScale;
    }
}