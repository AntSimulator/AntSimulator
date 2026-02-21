using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[CreateAssetMenu(fileName = "EventDefinition", menuName = "Scriptable Objects/EventDefinition")]
public class EventDefinition : ScriptableObject
{
    //얘는 섹터 이벤트에 적용됨
    
    [Header("기본 정보")] 
    public string eventId;
    public string title;
    [TextArea] public string description;

    [Header("영향 범위")] public EventScope scope = EventScope.Stocks;

    [Header("영향 대상 (적용 대상이 섹터인 경우 사용)")] public Sector sector;

    [Header("영향 대상 (적용 대상이 개별 주식인 경우 사용)")] public List<string> targetStockIds;

    [Header("확률 영향")] 
    public float probEffect;
    public bool isUp;

    [Header("변동폭 영향")] public float depthEffect;

    [Header("지속")] public int durationDays = 1;
    public int durationTick = 90;

    [Header("상폐")] 
    public bool delist = false;
    public List<string> delistStockIds;
    
    [Header("메타")] 
    public bool canBeCalendarEvent = true;
    public bool canBeHidden = true;
    public bool allowRepeatInRun = false;
    
    [Header("Kind")]
    public EventKind kind = EventKind.Normal;

}
public enum EventKind
{
    Normal,     // 뉴스
    Calendar,   // 캘린더 연출
    Ending      // 엔딩 등 특수
}


public enum EventScope
{
    MarketAll,
    Sector,
    Stocks
}

public enum Sector
{
    ENT,
    TECH,
    INSURANCE,
    FOOD,
    ENERGY,
    FINANCE,
    BIO,
    DEFENSE
}