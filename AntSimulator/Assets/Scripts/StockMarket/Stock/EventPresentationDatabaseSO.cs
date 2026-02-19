using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventPresentationDB", menuName = "Scriptable Objects/EventPresentationDatabaseSO")]
public class EventPresentationDatabaseSO : ScriptableObject
{
    public List<EventPresentationSO> items = new();

    private Dictionary<string, EventPresentationSO> _map;

    public EventPresentationSO Find(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return null;

        if (_map == null)
        {
            _map = new Dictionary<string, EventPresentationSO>();
            for (int i = 0; i < items.Count; i++)
            {
                var so = items[i];
                if (so == null || string.IsNullOrEmpty(so.eventId)) continue;
                _map[so.eventId] = so;
            }
        }

        _map.TryGetValue(eventId, out var found);
        return found;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _map = null; // 에디터에서 수정되면 캐시 리셋
    }
#endif
}