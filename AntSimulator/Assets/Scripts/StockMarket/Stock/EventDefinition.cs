using System.Collections.Generic;
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

    [Header("영향 대상")] public List<string> targetStockIds;

    [Header("확률 영향")] public float probEffect;

    [Header("변동폭 영향")] public float depthEffect;

    [Header("지속")] public int durationDays = 1;

    [Header("메타")] public bool isCalendarEvent = true;
}
