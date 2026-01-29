using UnityEngine;
using UnityEngine.UI;

public class ScrollRectHorizontalSync : MonoBehaviour
{
    [Header("Assign two ScrollRects")]
    [SerializeField] ScrollRect a;
    [SerializeField] ScrollRect b;

    bool _isSyncing;

    void Awake()
    {
        if (!a || !b) return;

        a.onValueChanged.AddListener(OnAChanged);
        b.onValueChanged.AddListener(OnBChanged);
    }

    void OnDestroy()
    {
        if (!a || !b) return;

        a.onValueChanged.RemoveListener(OnAChanged);
        b.onValueChanged.RemoveListener(OnBChanged);
    }

    void OnAChanged(Vector2 v)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        // 가로만 동기화
        b.horizontalNormalizedPosition = a.horizontalNormalizedPosition;

        _isSyncing = false;
    }

    void OnBChanged(Vector2 v)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        a.horizontalNormalizedPosition = b.horizontalNormalizedPosition;

        _isSyncing = false;
    }
}