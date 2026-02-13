using System;
using UnityEngine;

[Serializable]
public class HTSPost
{
    public string id;
    public int day;
    public int tickInDay;

    public string stockId;
    public HTSReactionDirection direction;

    public string title;
    public string body;
}
