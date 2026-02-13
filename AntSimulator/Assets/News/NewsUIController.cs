using TMPro;
using UnityEngine;

public class NewsUIController : MonoBehaviour
{
    [Header("Refs")]
    public MarketSimulator market;

    [Header("UI (Only content)")]
    public TMP_Text contentText;   // ✅ 이거 하나만 연결

    void OnEnable()
    {
        if (market != null)
            market.OnEventRevealed += HandleEventRevealed;
    }

    void OnDisable()
    {
        if (market != null)
            market.OnEventRevealed -= HandleEventRevealed;
    }

    void HandleEventRevealed(EventDefinition def, int day, int tick)
    {
        if (def == null || contentText == null) return;

        // description만 표시 (없으면 빈칸)
        // def.description 필드명이 다르면 여기만 바꿔
        contentText.text = def.description ?? "";
    }
}