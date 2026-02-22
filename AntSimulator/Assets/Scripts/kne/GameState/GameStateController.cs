using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;
using Player.Runtime;

public class GameStateController : MonoBehaviour
{
    private bool isRestoring = false;

    private IGameState currentState;
    public string currentStateName = "";
    public float stateTimer = 0f;
    public int currentDay = 0;
    public CalendarManager calendarUI;
    public TextMeshProUGUI stateInfoText;

    public EventManager eventManager;
    public EventDatabaseSO eventDatabase;
    public RunRecorder runRecorder;
    public MarketSimulator market;

    public AudioSource bgmSource;
    public AudioClip tickingSound;

    public System.Action<int> OnDayStarted;

    public StockSeedExporter seedExporter;
    private bool isGameOver = false;

    [Header("Ending Settings")]
    public int targetDay = 5;
    private long targetCash = 4000000;
    private long ecurrentCash = 0;
    private PlayerController _endingPlayer;

    [Header("Calendar Event Schedules")] public List<CalendarDayScheduleSO> calendarSchedules = new();

    [Header("PreMarket-off list")]
    public List<MonoBehaviour> preMarketOffControllers = new List<MonoBehaviour>();

    [Header("MarketOpen-off list")]
    public List<MonoBehaviour> marketopenOffControllers = new List<MonoBehaviour>();

    [Header("Settlement-off list")]
    public List<MonoBehaviour> settlementOffControllers = new List<MonoBehaviour>();


    void Start()
    {
        Application.targetFrameRate = 30;
        seedExporter.WriteSeedOnce();
        
        //?? ??? ??? ??? ??? ??
        var runEvents = RunEventGenerator.Generate(
            eventDatabase,
            totalDays: targetDay,
            calendarSchedules: calendarSchedules,
            hiddenCount: targetDay * 3);

        //??? ??? ??? + day 1 ?? ??
        eventManager.Init(runEvents);
        eventManager.OnDayStarted(currentDay);

        // ?? ??
        runRecorder.totalDays = targetDay;
        runRecorder.BeginRunAndWriteManifest();

        //????? ??? ???
        ChangeState(new PreMarketState(this));
        calendarUI?.HighLightToday(currentDay);
        
        // ?? ???? Day start ?? ?? 
        OnDayStarted?.Invoke(currentDay);
        
        _endingPlayer = FindObjectOfType<PlayerController>();

    }

    // Update is called once per frame
    void Update()
    {
        currentState?.Tick();

        if (!isGameOver && _endingPlayer != null && _endingPlayer.CurrentHp <= 0)
        {
            isGameOver = true;
            StartCoroutine(GameOverRoutine());
        }
        
    }

    public void ChangeState(IGameState newState)
    {
        currentState?.Exit();
        currentState = newState;
        stateTimer = 0f;

        UpdateControllerStates(newState);

        currentState?.Enter();

        if(SaveManager.Instance != null && isRestoring == false)
        {
            SaveManager.Instance.AutoSave();
            Debug.Log("�ڵ� ����Ǿ����ϴ�.");
        } 
    }

    public void NextDay()
    {
        //?? ?? ??
        eventManager.OnDayEnded();

        currentDay++;

        if (calendarUI != null)
        {
            calendarUI.HighLightToday(currentDay);
        }

        // ??? ? ?? ??
        eventManager.OnDayStarted(currentDay);

        OnDayStarted?.Invoke(currentDay);

        Debug.Log("?????? ????????????! ????: " + currentDay + "??");
    }

    public void LoadState(string stateName, float savedTime)
    {
        isRestoring = true;
        

        if (stateName == "PreMarketState")
        {
            ChangeState(new PreMarketState(this));
        }
        else if (stateName == "MarketOpenState")
        {
            ChangeState(new MarketOpenState(this));
        }else if (stateName == "SettlementState")
        {
            ChangeState(new SettlementState(this));
        }else if (stateName == "JailState")
        {
            ChangeState(new JailState(this));
        }

        stateTimer = savedTime;
        currentStateName = stateName;

        isRestoring = false;
    }

    public void StartDayTransition()
    {
        StartCoroutine(TransitionRoutine());
    }

    private System.Collections.IEnumerator TransitionRoutine()
    {
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        }

        NextDay();

        if (currentDay > targetDay)
        {
            ChangeState(new GameEndingState(this));
            ecurrentCash = _endingPlayer.GetCash();

            if(ecurrentCash >= targetCash)
            {
                SceneManager.LoadScene("GoodEndingScene");
            }
            else
            {
                SceneManager.LoadScene("BadEndingScene");
            }
        }
        else
        {
            ChangeState(new PreMarketState(this));
        }

        yield return new WaitForSecondsRealtime(0.5f);


        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeIn());
        }
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        }
        ChangeState(new GameEndingState(this));
        SceneManager.LoadScene("StarveEndingScene");
    }

    private void SetListEnabled(List<MonoBehaviour> list, bool isEnabled)
    {
        foreach (var controller in list)
        {
            if (controller != null)
            {
                controller.enabled = isEnabled;
            }
        }
    }

    private void UpdateControllerStates(IGameState newState)
    {
        SetListEnabled(preMarketOffControllers, true);
        SetListEnabled(marketopenOffControllers, true);
        SetListEnabled(settlementOffControllers, true);

        if (newState is PreMarketState)
        {
            SetListEnabled(preMarketOffControllers, false);
        }
        else if (newState is MarketOpenState)
        {
            SetListEnabled(marketopenOffControllers, false);
        }
        else if (newState is SettlementState)
        {
            SetListEnabled(settlementOffControllers, false);
        }
    }
}
