using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[CreateAssetMenu(fileName = "StockDefinition", menuName = "Scriptable Objects/StockDefinition")]
public class StockDefinition : ScriptableObject
{
    [Header("Starting Price")] public float basePrice = 10f;

    // [Header("상승 하락 확률 가중치")] 
    // public float eventProbWeight = 1.0f;
    // public float statementWeight = 1.0f;
    // public float newsWeight = 1.0f;
    //
    // [Header("상승 하락 폭 가중치")]
    // public float communityWeight = 1.0f;
    // public float statemetDepthWeight = 1.0f;
    // public float eventDepthWeight = 1.0f;

    [Header("상승 하락 최대 폭")]
    [Tooltip("최대 상승 폭")]
    public float maxUpPercent = 0.08f;

    [Tooltip("최대 하락 폭")]
    public float maxDownPercent = 0.08f;


    [Header("기본 정보")] 
    public string name;
    public Sector sector;

    // public string ticker;
}
