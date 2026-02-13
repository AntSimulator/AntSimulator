using UnityEngine;

public enum HTSReactionDirection
{
    Up,
    Down
}
[CreateAssetMenu(fileName = "HTSCommunitySO", menuName = "Scriptable Objects/HTSCommunitySO")]
public class HTSCommunitySO : ScriptableObject
{
    [Header("Target")] 
    public string stockId;
    public HTSReactionDirection direction;

    [Header("Text")] 
    public string title;
    public string body;
    
}
