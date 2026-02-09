using System;
using UnityEngine;

[Serializable]
public class CommunityPost
{
    public string id;
    public int day;
    public int tickInDay;

    public CommunityPostType type;

    public string title;
    public string body;

    public string relatedStockId;
    public string linkedEventId;

    public string templateId;
    
}
