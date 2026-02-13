using TMPro;
using UnityEngine;

public class HTSPostRowView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    public void Bind(HTSPost post)
    {
        if (titleText) titleText.text = post?.title ?? "";
        if (bodyText)
        {
            var b = post?.body ?? "";
            bodyText.text = b;
            bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(b)); // body 없으면 숨김
        }
    }
}