using TMPro;
using UnityEngine;

public class GlobalPostRowView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    public void Bind(string header, CommunityPost post)
    {
        if (headerText) headerText.text = header ?? "";
        if (titleText) titleText.text = post?.title ?? "";

        if (bodyText)
        {
            var b = post?.body ?? "";
            bodyText.text = b;
            bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(b));
        }
    }
}