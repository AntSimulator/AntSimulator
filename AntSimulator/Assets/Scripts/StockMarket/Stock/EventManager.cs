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

    public int ticksPerDay = 180;
    public int morningTick = 0;

    public IReadOnlyList<EventInstance> ActiveEvents => active;

    public void Init(List<EventInstance> runEvents)
    {
        this.runEvents = runEvents ?? new List<EventInstance>();
        active.Clear();
    }

    public void OnDayStarted(int day)
    {
        active.Clear();
        if (runEvents == null) return;

        var todayList = runEvents.Where(x => x != null && x.startDay == day).ToList();
        if (todayList.Count == 0) return;

        // "랜덤 공개" 이벤트 중 하나를 morningTick에 배치하고 싶으면 유지
        // (캘린더 고정 이벤트는 제외해야 함)
        var randomRevealList = todayList.Where(e => e.revealTickInDay < 0).ToList();
        int morningIdx = (randomRevealList.Count > 0)
            ? UnityEngine.Random.Range(0, randomRevealList.Count)
            : -1;

        for (int i = 0; i < todayList.Count; i++)
        {
            var inst = todayList[i];
            if (inst == null) continue;

            // ✅ ticks-only: durationTicks만 사용 (SO에서 이미 채워져 있어야 함)
            int durTicks = inst.durationTicks > 0 ? inst.durationTicks : 60;
            inst.remainingTicks = durTicks;

            inst.revealed = false;
            inst.delistApplied = false;

            // ✅ 핵심: 캘린더 이벤트(= revealTickInDay >= 0)는 절대 덮어쓰지 않음
            if (inst.revealTickInDay < 0)
            {
                // 랜덤 공개 이벤트만 morningTick 옵션 적용
                bool isMorningPick = (morningIdx >= 0 && ReferenceEquals(inst, randomRevealList[morningIdx]));
                inst.revealTickInDay = isMorningPick
                    ? Mathf.Clamp(morningTick, 0, Mathf.Max(0, ticksPerDay - 1))
                    : UnityEngine.Random.Range(0, Mathf.Max(1, ticksPerDay));
            }
            else
            {
                // 안전 클램프 (slot이 범위 밖이면 고쳐줌)
                inst.revealTickInDay = Mathf.Clamp(inst.revealTickInDay, 0, Mathf.Max(0, ticksPerDay - 1));
            }

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