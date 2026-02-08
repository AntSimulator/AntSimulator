using System;
using UnityEngine;

[Serializable]
public class EventInstance
{
    public string eventId;
    public int startDay;
    public int durationDays;
    public int remainingDays;
    //public bool isHidden;

    public int revealTickInDay;
    public bool revealed;

    public bool delistApplied;

    public EventInstance(string eventId, int startDay, int durationDays, bool isHidden)
    {
        this.eventId = eventId;
        this.startDay = startDay;
        this.durationDays = durationDays;
        this.remainingDays = durationDays;
        //this.isHidden = isHidden;
        revealed = false;
        revealTickInDay = 0;
        delistApplied = false;
    }
}