using UnityEngine;
using UnityEngine.UI;
using Player.Runtime;

public class HpLowWarningFlicker : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerController player;   // 인스펙터에 PlayerController 드래그
    [SerializeField] private Image overlay;             // 전체화면 빨간 이미지 (alpha만 조절)

    [Header("Threshold")]
    [SerializeField] private int lowHpThreshold = 10;

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed = 6f;   // 커질수록 빨리 깜빡임
    [SerializeField] private float maxAlpha = 0.35f;    // 최대 진하기

    private bool _active;

    void Awake()
    {
        if (overlay != null)
        {
            var c = overlay.color;
            c.a = 0f;
            overlay.color = c;
        }
    }

    void OnEnable()
    {
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (player != null) player.OnHpChanged += HandleHpChanged;
    }

    void OnDisable()
    {
        if (player != null) player.OnHpChanged -= HandleHpChanged;
        SetAlpha(0f);
        _active = false;
    }

    void Update()
    {
        if (!_active || overlay == null) return;

        // 0~1 왕복(sin) -> alpha
        float t = (Mathf.Sin(Time.unscaledTime * flickerSpeed) + 1f) * 0.5f;
        SetAlpha(t * maxAlpha);
    }

    void HandleHpChanged(int currentHp, int maxHp)
    {
        _active = currentHp > 0 && currentHp <= lowHpThreshold;

        if (!_active)
            SetAlpha(0f);
    }

    void SetAlpha(float a)
    {
        if (overlay == null) return;
        var c = overlay.color;
        c.a = Mathf.Clamp01(a);
        overlay.color = c;
    }
}