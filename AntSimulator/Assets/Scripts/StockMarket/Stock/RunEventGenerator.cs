using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RunEventGenerator
{
    public static List<EventInstance> Generate(EventDatabaseSO db, int totalDays, List<CalendarDayScheduleSO> calendarSchedules, int hiddenCount)
    {
        var result = new List<EventInstance>();
        if (db == null || db.events == null || db.events.Count == 0) return result;

        if (calendarSchedules != null && calendarSchedules.Count > 0)
        {
             var map = calendarSchedules
                .Where(s => s != null)
                .GroupBy(s => s.dayOfWeek)
                .ToDictionary(g => g.Key, g => g.First()); // 요일당 1개만 쓰게 (중복 방지)

            for (int day = 1; day <= totalDays; day++)
            {
                var dow = ToDow(day);
                if (!map.TryGetValue(dow, out var schedule) || schedule == null) continue;
                if (schedule.slots == null || schedule.slots.Count == 0) continue;

                foreach (var slot in schedule.slots)
                {
                    if (slot == null) continue;

                    // good/bad 중 하나 확정
                    bool isGood = Random.value < Mathf.Clamp01(slot.goodChance);
                    string eventId = isGood ? slot.goodEventId : slot.badEventId;

                    if (string.IsNullOrEmpty(eventId))
                    {
                        Debug.LogError($"[RunEventGenerator] Empty eventId in {schedule.name} ({dow}) slot.");
                        continue;
                    }

                    var def = db.FindById(eventId);
                    if (def == null)
                    {
                        Debug.LogError($"[RunEventGenerator] EventDefinition not found: {eventId} (from {schedule.name})");
                        continue;
                    }

                    int duration = (slot.overrideDurationDays > 0) ? slot.overrideDurationDays : def.durationDays;

                    // revealTickInDay 같은 건 EventInstance에 필드가 있어야 저장됨.
                    // 너희 EventInstance가 revealTickInDay가 없다면, 생성 직후 EventManager가 default로 잡고 있거나,
                    // EventInstance에 revealTickInDay 필드를 추가해서 저장하는 게 베스트.
                    var inst = new EventInstance(def.eventId, day, duration, isHidden: slot.isHidden);

                    // ✅ (권장) EventInstance에 revealTickInDay 필드가 있으면 여기서 넣어라:
                    // inst.revealTickInDay = slot.revealTickInDay;
                    inst.durationTicks = 60;
                    result.Add(inst);
                }
            }
        }
        var hiddenPool = db.events
            .Where(e => e != null && e.canBeHidden)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(hiddenCount)
            .ToList();

        foreach (var def in hiddenPool)
        {
            int day = UnityEngine.Random.Range(1, totalDays + 1);
            result.Add(new EventInstance(def.eventId, day, def.durationTick, isHidden: true));
        }

        return result;
    }
    
    
    /// <summary>
    /// day(1..N)를 월~금으로 매핑. (너 게임이 4일이면 Tue~Fri 같은 식으로 시작 요일을 바꿀 수도 있음)
    /// </summary>
    private static CalendarDayOfWeek ToDow(int day)
    {
        // 기본: day1=Mon, day2=Tue ... day5=Fri, 그 이후는 반복
        int idx = (day - 1) % 5;
        return (CalendarDayOfWeek)idx;
    }
    
}