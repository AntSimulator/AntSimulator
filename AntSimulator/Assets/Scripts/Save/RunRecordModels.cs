using System;
using System.Collections.Generic;

[Serializable]
public class RunManifestData
{
    public string runId;
    public int totalDays;
    public string createdAtUtc;

    public List<RunManifestStock> stocks = new();
    public List<RunManifestEvent> events = new();
}

[Serializable]
public class RunManifestStock
{
    public string stockId;
    public float basePrice;
}

[Serializable]
public class RunManifestEvent
{
    public string eventId;
    public int startDay;
    public int durationDays;
    public bool isHidden;
}

[Serializable]
public class TickLine
{
    public int tick;
    public int day;
    public string t; // ISO8601
    public List<TickPoint> prices = new();
}

[Serializable]
public class TickPoint
{
    public string id;
    public float p;
    public float v;
}