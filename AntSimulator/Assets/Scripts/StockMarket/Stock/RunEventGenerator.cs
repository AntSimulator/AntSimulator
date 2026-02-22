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

        // ---------------------------
        // 1) Calendar schedules: revealTickInDay는 slot 값 "고정"
        // ---------------------------
        if (calendarSchedules != null && calendarSchedules.Count > 0)
        {
            var map = calendarSchedules
                .Where(s => s != null)
                .GroupBy(s => s.dayOfWeek)
                .ToDictionary(g => g.Key, g => g.First()); // 요일당 1개만

            for (int day = 1; day <= totalDays; day++)
            {
                var dow = ToDow(day);
                if (!map.TryGetValue(dow, out var schedule) || schedule == null) continue;
                if (schedule.slots == null || schedule.slots.Count == 0) continue;

                foreach (var slot in schedule.slots)
                {
                    if (slot == null) continue;

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

                    // 레거시: durationDays는 있어도 됨(프리팹/세이브 호환)
                    int durationDays = (slot.overrideDurationDays > 0) ? slot.overrideDurationDays : def.durationDays;

                    var inst = new EventInstance(def.eventId, day, durationDays, isHidden: slot.isHidden);

                    // ✅ 캘린더 이벤트만: slot이 정한 공개 tick을 "그대로" 넣는다
                    inst.revealTickInDay = slot.revealTickInDay;

                    // ✅ ticks-only: durationTicks는 SO값(=def.durationTick)을 사용
                    inst.durationTicks = def.durationTick > 0 ? def.durationTick : 60;

                    result.Add(inst);
                }
            }
        }

        // ---------------------------
        // 2) Hidden events: revealTickInDay는 랜덤(= -1로 두고 EventManager가 처리)
        // ---------------------------
        var hiddenPool = db.events
            .Where(e => e != null && e.canBeHidden)
            .OrderBy(_ => Random.value)
            .Take(hiddenCount)
            .ToList();

        foreach (var def in hiddenPool)
        {
            int day = Random.Range(1, totalDays + 1);

            // ✅ 여기 버그였음: durationTick을 durationDays 자리에 넣으면 안 됨
            var inst = new EventInstance(def.eventId, day, def.durationDays, isHidden: true);

            // ticks-only: durationTicks는 SO값
            inst.durationTicks = def.durationTick > 0 ? def.durationTick : 60;

            // revealTickInDay는 -1 유지 → EventManager가 랜덤으로 박음
            // inst.revealTickInDay = -1;

            result.Add(inst);
        }

        return result;
    }

    private static CalendarDayOfWeek ToDow(int day)
    {
        int idx = (day - 1) % 5; // day1=Mon ...
        return (CalendarDayOfWeek)idx;
    }
}