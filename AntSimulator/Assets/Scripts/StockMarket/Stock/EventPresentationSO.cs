using System.Collections.Generic;
using UnityEngine;

public enum EventPresentationType
{
    News,
    Calendar,
    Ending
}

[CreateAssetMenu(fileName = "EventPresentation", menuName = "Scriptable Objects/EventPresentationSO")]
public class EventPresentationSO : ScriptableObject
{
    [Header("Key (must match EventDefinition.eventId)")]
    public string eventId;

    [Header("Which popup to use")]
    public EventPresentationType presentationType = EventPresentationType.News;

    [Header("Common (Optional)")]
    public Sprite image; // 이벤트 이미지

    [Header("NEWS (Optional fallback text)")]
    [TextArea] public string newsFallbackBody;

    [Header("CALENDAR (Stardew-like lines)")]
    public List<string> calendarLines = new();

    [Header("Calendar Typewriter")]
    [Tooltip("Characters per second")]
    public float charsPerSecond = 45f;

    [Tooltip("Auto-advance after this many seconds (0 = wait for click)")]
    public float autoAdvanceSeconds = 0f;
}