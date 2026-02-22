using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [SerializeField] private List<EventInstance> runEvents = new();
    public List<EventInstance> GetRunEvents() => runEvents;

    public EventDatabaseSO db;

    private readonly List<EventInstance> active = new();

    public int ticksPerDay = 300;
    public int morningTick = 0;

    public IReadOnlyList<EventInstance> ActiveEvents => active;

    public void Init(List<EventInstance> runEvents)
    {
        this.runEvents = runEvents ?? new List<EventInstance>();
        active.Clear();
    }

    public void OnDayStarted(int day)
    {
        if (runEvents == null) return;

        var todayList = runEvents.Where(x => x != null && x.startDay == day).ToList();
        if (todayList.Count == 0) return;

        int morningIdx = UnityEngine.Random.Range(0, todayList.Count);

        for (int i = 0; i < todayList.Count; i++)
        {
            var inst = todayList[i];
            if (inst == null) continue;

            // ✅ durationTicks가 0이면 durationDays * ticksPerDay로 자동 변환(구 SO 호환)
            int fallbackTicks = Mathf.Max(1, Mathf.Max(1, ticksPerDay));
            int durTicks = inst.durationTicks > 0 ? inst.durationTicks : fallbackTicks;

            inst.remainingTicks = durTicks;
            inst.revealed = false;
            inst.delistApplied = false;

            inst.revealTickInDay = (i == morningIdx)
                ? morningTick
                : UnityEngine.Random.Range(0, Mathf.Max(1, ticksPerDay));

            if (!active.Contains(inst))
                active.Add(inst);
        }
    }

    /// <summary>
    /// MarketSimulator에서 매 tick마다 호출해줘야 함 (너는 이미 호출 중)
    /// </summary>
    public void OnTick()
    {
        for (int i = 0; i < active.Count; i++)
        {
            var inst = active[i];
            if (inst == null) continue;

            // ✅ 공개 전에는 영향도 없고, 시간도 안 줄임
            if (!inst.revealed) continue;

            inst.remainingTicks--;
        }

        // ✅ 공개된 이벤트 중 시간이 끝난 것 제거
        active.RemoveAll(e => e != null && e.revealed && e.remainingTicks <= 0);
    }

    // ✅ DayEnd는 ticks 시스템에서는 굳이 건드릴 필요 없음 (남겨도 되는데, 비우는 게 안전)
    public void OnDayEnded()
    {
        // nothing (tick 기반 수명 관리)
    }

    public (float probSum, float depthSum) GetEffectsForStock(string stockId, Sector stockSector)
    {
        float probSum = 0f;
        float depthMul = 1f;

        foreach (var inst in active)
        {
            if (inst == null) continue;
            if (!inst.revealed) continue;

            var def = db != null ? db.FindById(inst.eventId) : null;
            if (def == null) continue;

            bool affects = def.scope switch
            {
                EventScope.MarketAll => true,
                EventScope.Sector => def.sector == stockSector,
                EventScope.Stocks => def.targetStockIds == null || def.targetStockIds.Count == 0 ||
                                     def.targetStockIds.Contains(stockId),
                _ => false
            };

            if (!affects) continue;

            // ✅ isUp 기준으로 확률 퍼센트포인트를 +/- 적용
            probSum += def.isUp ? def.probEffect : -def.probEffect;

            // ✅ 폭은 항상 누적(증가분으로)
            depthMul *= (1f + def.depthEffect);
        }

        return (probSum, depthMul - 1f);
    }

    public List<EventInstance> GetEventsStartingOnDay(int day)
    {
        if (runEvents == null) return new List<EventInstance>();
        return runEvents.Where(e => e != null && e.startDay == day).ToList();
    }

    public EventInstance PickTomorrowForeshadow(int today)
    {
        var list = GetEventsStartingOnDay(today + 1);
        if (list.Count == 0) return null;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
}