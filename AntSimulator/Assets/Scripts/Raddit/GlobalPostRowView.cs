using TMPro;
using UnityEngine;

public class GlobalPostRowView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text bodyText;

    public void Bind(string header, CommunityPost post)
    {
        if (headerText)
        {
            headerText.text = string.IsNullOrWhiteSpace(header) ? "#GLOBAL" : $"#{header}";
            headerText.gameObject.SetActive(true);
        }

        if (bodyText)
        {
            var b = post?.body ?? "";
            bodyText.text = b;

            // body가 비면 숨김(원하면 줄바꿈 등으로 처리 가능)
            bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(b));
        }
    }
}