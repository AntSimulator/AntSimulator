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

        var todayList = runEvents.Where(x => x.startDay == day).ToList();
        if(todayList.Count == 0) return;
        int morningIdx = UnityEngine.Random.Range(0, todayList.Count);
        for(int i = 0; i<todayList.Count; i++)
        {
            var inst = todayList[i];
            if(inst == null)continue;
            inst.remainingDays = inst.durationDays;
            inst.revealed = false;
            inst.delistApplied = false;
            if (i == morningIdx)
            {
                inst.revealTickInDay = morningTick;
            }
            else
            {
                inst.revealTickInDay = UnityEngine.Random.Range(0, Mathf.Max(1, ticksPerDay));
            }

            if (!active.Contains(inst))
            {
                active.Add(inst);
            }
        }
    }

    public void OnDayEnded()
    {
        for (int i = 0; i < active.Count; i++)
            active[i].remainingDays--;

        active.RemoveAll(e => e.remainingDays <= 0);
    }

    /// <summary>
    /// probSum: (0.xx) 퍼센트포인트 합산 (0.10 = +10%p)
    /// depthSum: 여러 이벤트 depthEffect는 (1+effect)를 곱 누적하고 마지막에 -1 (증가분)
    /// </summary>
    public (float probSum, float depthSum) GetEffectsForStock(string stockId, Sector stockSector)
    {
        float probSum = 0f;
        float depthMul = 1f;

        foreach (var inst in active)
        {
            var def = db != null ? db.FindById(inst.eventId) : null;
            if (def == null) continue;
            if(!inst.revealed) continue;

            bool affects = def.scope switch
            {
                EventScope.MarketAll => true,
                EventScope.Sector => def.sector == stockSector,
                EventScope.Stocks => def.targetStockIds == null || def.targetStockIds.Count == 0 ||
                                     def.targetStockIds.Contains(stockId),
                _ => false
            };

            if (!affects) continue;

            if (def.isUp == true)
            {
                probSum += def.probEffect;  
            }
            else
            {
                probSum -= def.probEffect;
            }
                          // 합산 (0.10 = +10%p)
                          
            depthMul *= (1f + def.depthEffect);      // 곱 누적
        }

        float depthSum = depthMul - 1f;              // 증가분으로 환원
        return (probSum, depthSum);
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