using System;
using System.Collections;
using System.Collections.Generic;
using Player.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Work
{
    public class WorkController : MonoBehaviour
    {
        private const float WorkTickIntervalSeconds = 0.1f; // 10 ticks per second

        [Serializable]
        public class WorkOption
        {
            [Min(0)] public long rewardCash = 10000;
        }

        [Header("References")]
        [SerializeField] private Button workButton;
        [SerializeField] private GameStateController gameStateController;
        [SerializeField] private PlayerController playerController;

        [Header("Work Options")]
        [SerializeField] private List<WorkOption> workOptions = new();
        [SerializeField, Min(0)] private int selectedWorkIndex;
        private WaitForSecondsRealtime fastForwardWait;
        private Coroutine fastForwardCoroutine;
        private bool isFastForwarding;

        private void Reset()
        {
            workButton = GetComponent<Button>();
            gameStateController = FindFirstObjectByType<GameStateController>();
            playerController = FindFirstObjectByType<PlayerController>();
        }

        private void Awake()
        {
            if (gameStateController == null)
            {
                gameStateController = FindFirstObjectByType<GameStateController>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            fastForwardWait = new WaitForSecondsRealtime(WorkTickIntervalSeconds);
        }

        private void OnEnable()
        {
            if (workButton != null)
            {
                workButton.onClick.AddListener(HandleWorkButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (workButton != null)
            {
                workButton.onClick.RemoveListener(HandleWorkButtonClicked);
            }

            if (fastForwardCoroutine != null)
            {
                StopCoroutine(fastForwardCoroutine);
                fastForwardCoroutine = null;
                isFastForwarding = false;
            }
        }

        public void HandleWorkButtonClicked()
        {
            ExecuteWork(selectedWorkIndex);
        }

        public void ExecuteWork(int index)
        {
            if (workOptions == null || workOptions.Count == 0)
            {
                Debug.LogWarning("[Work] Work options are empty.");
                return;
            }

            if (index < 0 || index >= workOptions.Count)
            {
                Debug.LogWarning($"[Work] Invalid index={index}");
                return;
            }

            var option = workOptions[index];
            if (option == null)
            {
                Debug.LogWarning($"[Work] Work option is null. index={index}");
                return;
            }

            if (gameStateController == null)
            {
                Debug.LogWarning("[Work] GameStateController is missing.");
                return;
            }

            if (isFastForwarding)
            {
                Debug.LogWarning("[Work] Fast-forward is already running.");
                return;
            }

            if (playerController != null && option.rewardCash > 0)
            {
                playerController.Cash += option.rewardCash;
            }

            fastForwardCoroutine = StartCoroutine(FastForwardToSettlement());

            Debug.Log($"[Work] index={index} reward+={option.rewardCash} cash={playerController?.Cash ?? 0}");
        }

        private IEnumerator FastForwardToSettlement()
        {
            isFastForwarding = true;
            int startDay = gameStateController.currentDay;

            try
            {
                while (gameStateController != null &&
                       gameStateController.currentDay == startDay &&
                       gameStateController.currentStateName != "SettlementState")
                {
                    if (gameStateController.market == null)
                    {
                        Debug.LogWarning("[Work] MarketSimulator is missing.");
                        yield break;
                    }

                    if (gameStateController.currentStateName == "PreMarketState")
                    {
                        gameStateController.ChangeState(new MarketOpenState(gameStateController));
                        gameStateController.market.simulateTicks = false;
                        continue;
                    }

                    if (gameStateController.currentStateName != "MarketOpenState")
                    {
                        Debug.LogWarning($"[Work] Fast-forward aborted in state={gameStateController.currentStateName}");
                        yield break;
                    }

                    gameStateController.market.simulateTicks = false;
                    gameStateController.market.TickOnce();

                    if (gameStateController.market.IsDayTickFinished)
                    {
                        gameStateController.ChangeState(new SettlementState(gameStateController));
                        break;
                    }

                    yield return fastForwardWait;
                }
            }
            finally
            {
                fastForwardCoroutine = null;
                isFastForwarding = false;
            }
        }
    }
}

