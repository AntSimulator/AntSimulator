using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RunEventGenerator
{
 public static List<EventInstance> Generate(
    EventDatabaseSO db,
    int totalDays,
    List<CalendarDayScheduleSO> calendarSchedules,
    int hiddenCount)
{
    var result = new List<EventInstance>();
    if (db == null || db.events == null || db.events.Count == 0) return result;

    // 1) 요일 -> 슬롯 리스트(전부 합치기)
    Dictionary<CalendarDayOfWeek, List<CalendarEventSlot>> map = null;

    if (calendarSchedules != null && calendarSchedules.Count > 0)
    {
        map = calendarSchedules
            .Where(s => s != null)
            .GroupBy(s => s.dayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(s => s.slots ?? new List<CalendarEventSlot>()).ToList()
            );
    }

    // 2) 캘린더 이벤트 생성
    if (map != null)
    {
        for (int day = 1; day <= totalDays; day++)
        {
            var dow = ToDow(day); // ✅ 1-based 보정된 ToDow 사용

            if (!map.TryGetValue(dow, out var slots) || slots == null || slots.Count == 0)
                continue;

            foreach (var slot in slots)
            {
                if (slot == null) continue;

                bool isGood = Random.value < Mathf.Clamp01(slot.goodChance);
                string eventId = isGood ? slot.goodEventId : slot.badEventId;
                if (string.IsNullOrEmpty(eventId)) continue;

                var def = db.FindById(eventId);
                if (def == null) continue;

                int durationDays = (slot.overrideDurationDays > 0) ? slot.overrideDurationDays : def.durationDays;

                var inst = new EventInstance(def.eventId, day, durationDays, isHidden: slot.isHidden);

                inst.revealTickInDay = slot.revealTickInDay;                // 고정
                inst.durationTicks = def.durationTick > 0 ? def.durationTick : 60;

                result.Add(inst);
            }
        }
    }

    // 3) Hidden events (그대로)
    var hiddenPool = db.events
        .Where(e => e != null && e.canBeHidden)
        .OrderBy(_ => Random.value)
        .Take(hiddenCount)
        .ToList();

    foreach (var def in hiddenPool)
    {
        int day = Random.Range(1, totalDays + 1);
        var inst = new EventInstance(def.eventId, day, def.durationDays, isHidden: true);
        inst.durationTicks = def.durationTick > 0 ? def.durationTick : 60;
        // revealTickInDay = -1 유지
        result.Add(inst);
    }

    return result;
}

private static CalendarDayOfWeek ToDow(int day)
{
    int idx = ((day - 1) % 5) + 1;   // ✅ 1..5
    return (CalendarDayOfWeek)idx;
} 
      
}