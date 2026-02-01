using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [SerializeField] private List<EventInstance> runEvents = new();
    public List<EventInstance> GetRunEvents() => runEvents;

    public EventDatabaseSO db;

    private readonly List<EventInstance> active = new();

    public void Init(List<EventInstance> runEvents)
    {
        this.runEvents = runEvents ?? new List<EventInstance>();
        active.Clear();
    }

    public void OnDayStarted(int day)
    {
        if (runEvents == null) return;
        foreach (var inst in runEvents.Where(x => x.startDay == day))
            active.Add(inst);
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
}