using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CalendarDayOfWeek
{
    Mon = 1,
    Tue = 2,
    Wed = 3,
    Thu = 4,
    Fri = 5
}

[Serializable]
public class CalendarEventSlot
{
    [Tooltip("good evnet Id")] public string goodEventId;

    [Tooltip("bad event Id")] public string badEventId;

    [Range(0f, 1f)] public float goodChance = 0.5f;

    [Tooltip("언제 공개되는지")] public int revealTickInDay = 30;

    [Tooltip("지속 일수 (0이면 event definiton의 duration days 사용)")]
    public int overrideDurationDays = 0;

    [Tooltip("숨김 여부 (false로 설정해둘것)")] public bool isHidden = false;


}

[CreateAssetMenu(fileName = "CalendarDayScheduleSO", menuName = "Scriptable Objects/CalendarDayScheduleSO")]
public class CalendarDayScheduleSO : ScriptableObject
{
    public CalendarDayOfWeek dayOfWeek;

    [Tooltip("이 요일에 동시에 터질 수 있는 이벤트 슬롯")] public List<CalendarEventSlot> slots = new();
}
