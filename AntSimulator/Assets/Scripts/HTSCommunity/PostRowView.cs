using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HTSPostRowView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [SerializeField] private LayoutElement bodyLayout;

    public void Bind(HTSPost post)
    {
        // 1) title은 무조건 보여주기
        if (titleText)
        {
            var t = post?.title ?? "";
            titleText.text = t;
            // 혹시 프리팹에서 title이 꺼져있을 수도 있으니 강제로 켬
            if (!titleText.gameObject.activeSelf) titleText.gameObject.SetActive(true);
        }

        // 2) body는 "없으면 안 보이게" = 텍스트 비우고 높이를 0으로
        if (bodyText)
        {
            var b = post?.body;

            bool hasBody = !string.IsNullOrWhiteSpace(b);

            bodyText.text = hasBody ? b : "";

            // GameObject를 끄지 말고, 레이아웃만 0으로 만드는 게 안전함
            if (bodyLayout != null)
            {
                bodyLayout.ignoreLayout = !hasBody;
                bodyLayout.preferredHeight = hasBody ? -1f : 0f; // -1이면 TMP/ContentSizeFitter가 계산
            }

            // 그래도 시각적으로 안 보이게 하고 싶으면 알파만 0 처리(선택)
            bodyText.alpha = hasBody ? 1f : 0f;
        }
    }
}