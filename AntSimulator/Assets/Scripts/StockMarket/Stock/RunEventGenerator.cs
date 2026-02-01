using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RunEventGenerator
{
    public static List<EventInstance> Generate(EventDatabaseSO db, int totalDays, int calendarCount, int hiddenCount)
    {
        var result = new List<EventInstance>();
        if (db == null || db.events == null || db.events.Count == 0) return result;

        var calendarPool = db.events
            .Where(e => e != null && e.canBeCalendarEvent)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(calendarCount)
            .ToList();

        for (int i = 0; i < calendarPool.Count; i++)
        {
            var def = calendarPool[i];
            int day = Mathf.Clamp(i + 1, 1, totalDays);
            result.Add(new EventInstance(def.eventId, day, def.durationDays, isHidden: false));
        }

        var hiddenPool = db.events
            .Where(e => e != null && e.canBeHidden)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(hiddenCount)
            .ToList();

        foreach (var def in hiddenPool)
        {
            int day = UnityEngine.Random.Range(1, totalDays + 1);
            result.Add(new EventInstance(def.eventId, day, def.durationDays, isHidden: true));
        }

        return result;
    }
}