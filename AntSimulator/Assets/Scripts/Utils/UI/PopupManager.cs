using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils.UI
{
    public class PopupManager : MonoBehaviour
    {
    [Serializable]
    public class PopupEntry
    {
        public string id;          // 예: "Popup_HTS"
        public GameObject root;    // 예: Background/Popup_HTS
    }

    [SerializeField] private List<PopupEntry> popups = new();

    private Dictionary<string, GameObject> map;

    private void Awake()
    {
        map = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        foreach (var p in popups)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.id) || p.root == null) continue;
            map[p.id] = p.root;
        }
    }

    public void Open(string id)
    {
        if (map == null || !map.TryGetValue(id, out var go) || go == null) return;
        go.SetActive(true);
    }

    public void Close(string id)
    {
        if (map == null || !map.TryGetValue(id, out var go) || go == null) return;
        go.SetActive(false);
    }

    public void CloseAll()
    {
        if (map == null) return;
        foreach (var kv in map)
        {
            if (kv.Value != null) kv.Value.SetActive(false);
        }
    }

    // "하나만 열기" 규칙 원하면 이걸 쓰면 됨
    public void OpenExclusive(string id)
    {
        CloseAll();
        Open(id);
    }
}
}
