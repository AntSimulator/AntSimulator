using UnityEngine;

public class MarketBootstrapper : MonoBehaviour
{
    public GameStateController game;
    public MarketSimulator market;
    public EventManager events;
    public RunRecorder recorder;

    void Reset()
    {
        game = GetComponent<GameStateController>();
        market = GetComponent<MarketSimulator>();
        events = GetComponent<EventManager>();
        recorder = GetComponent<RunRecorder>();
    }

    void Awake()
    {
        if (!game) game = GetComponent<GameStateController>();
        if (!market) market = GetComponent<MarketSimulator>();
        if (!events) events = GetComponent<EventManager>();
        if (!recorder) recorder = GetComponent<RunRecorder>();

        if (game != null)
        {
            game.eventManager = events;
            game.runRecorder = recorder;
        }

        if (market != null)
        {
            market.eventManager = events;
            market.runRecorder = recorder;
            market.gameStateController = game;
        }

        if (recorder != null)
        {
            recorder.gameStateController = game;
            recorder.marketSimulator = market;
            recorder.eventManager = events;
        }
    }
}