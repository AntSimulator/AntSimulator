using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;
using Player;

public class GameStateController : MonoBehaviour
{
    public SaveManager saveManager;
    private bool isRestoring = false;

    private IGameState currentState;
    public string currentStateName = "";
    public float stateTimer = 0f;
    public int currentDay = 1;
    public CalendarManager calendarUI;
    public TextMeshProUGUI stateInfoText;

    public EventManager eventManager;
    public EventDatabaseSO eventDatabase;
    public RunRecorder runRecorder;

    public System.Action<int> OnDayStarted;

    public StockSeedExporter seedExporter;

    [Header("Ending Settings")]
    public int targetDay = 5;
    private long targetCash = 4000000;
    private long ecurrentCash = 0;
    private PlayerController _endingPlayer;


    void Start()
    {
        Application.targetFrameRate = 30;
        seedExporter.WriteSeedOnce();
        
        //?? ??? ??? ??? ??? ??
        var runEvents = RunEventGenerator.Generate(
            eventDatabase,
            totalDays: targetDay,
            calendarCount: targetDay,
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

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame) ChangeState(new MarketOpenState(this));
        if (keyboard.digit2Key.wasPressedThisFrame) ChangeState(new JailState(this));
        if (keyboard.digit3Key.wasPressedThisFrame) ChangeState(new SettlementState(this));
        
    }

    public void ChangeState(IGameState newState)
    {
        currentState?.Exit();
        currentState = newState;
        stateTimer = 0f;
        currentState?.Enter();

        if(saveManager != null && isRestoring == false)
        {
            saveManager.AutoSave();
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

        yield return new WaitForSeconds(0.5f);


        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeIn());
        }
    }
}
