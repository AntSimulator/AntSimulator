using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RainbowText : MonoBehaviour
{
    private TextMeshProUGUI textMesh; // TMP 사용 시
    private Text legacyText; // 일반 Text 사용 시
    public float speed = 0.50f; // 색상 변화 속도

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        legacyText = GetComponent<Text>();
    }

    void Update()
    {
        // 시간에 따라 0~1 사이의 값을 순환 (HSV 색상 모델 활용)
        float hue = (Time.time * speed) % 1.0f;
        Color rainbowColor = Color.HSVToRGB(hue, 1.0f, 1.0f);

        // 색상 적용
        if (textMesh != null) textMesh.color = rainbowColor;
        if (legacyText != null) legacyText.color = rainbowColor;
    }
}