using UnityEngine;

public enum CommunityPostType
{
    Info,
    Brag,
    Loss,
    Meme,
    EventReaction
}
[CreateAssetMenu(fileName = "CommunityPostSO", menuName = "Scriptable Objects/CommunityPostSO")]
public class CommunityPostSO : ScriptableObject
{
    public string templateId;
    public CommunityPostType type;
    [Header("Text")] 
    [TextArea] public string title;
    [TextArea(1, 10)] public string body;

    [Header("Weights")]
    [Range(0.01f, 10f)] public float weight = 1f;

    [Header("Tagging")]
    public bool allowAttachStockId;
    public bool allowAttachEventId;
}
