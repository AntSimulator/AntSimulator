using UnityEngine;


[CreateAssetMenu(fileName = "CommunityPostSO", menuName = "Scriptable Objects/CommunityPostSO")]
public class CommunityPostSO : ScriptableObject
{
    [Header("Link (1:1)")]
    public string eventId; 
    
    
    [Header("Text")] 
    [TextArea] public string title;
    [TextArea] public string body;
}
