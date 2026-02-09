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

    [Header("Volume Model (per-stock)")]
    public float volumeBoostK = 25f;          // 등락률이 거래량을 얼마나 키우는지
    public float volumeNoiseMin = 0.7f;       // 거래량 랜덤폭
    public float volumeNoiseMax = 1.3f;

    [Header("Event Sensitivity (per-stock)")]
    public float eventToVolatility = 0.3f;    // 이벤트가 변동성을 얼마나 올리는지
    public float eventToVolume = 0.5f;        // 이벤트가 거래량을 얼마나 올리는지

    [Header("Community Sensitivity (per-stock)")]
    public float communitySensitivity = 1.0f; // 급등락/거래량 폭발 시 글이 얼마나 늘어나는지

    [Header("기본 정보")] 
    public string stockId;
    public Sector sector;
    public string displayName;
    public float floatShares;
    public long avgDailyVolume;
    public float noiseScale = 1f;


    // public string ticker;
}
