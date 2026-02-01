using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventDatabase", menuName = "Scriptable Objects/EventDatabase")]
public class EventDatabaseSO : ScriptableObject
{
    public List<EventDefinition> events = new();

    public EventDefinition FindById(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return null;
        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];
            if (e != null && e.eventId == eventId) return e;
        }
        return null;
    }
}