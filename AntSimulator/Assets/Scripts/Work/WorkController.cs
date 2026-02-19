using System;
using System.Collections.Generic;
using Player.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Work
{
    public class WorkController : MonoBehaviour
    {
        [Serializable]
        public class WorkOption
        {
            [Min(0f)] public float timeToPassSeconds = 10f;
            [Min(0)] public long rewardCash = 10000;
        }

        [Header("References")]
        [SerializeField] private Button workButton;
        [SerializeField] private GameStateController gameStateController;
        [SerializeField] private PlayerController playerController;

        [Header("Work Options")]
        [SerializeField] private List<WorkOption> workOptions = new();
        [SerializeField, Min(0)] private int selectedWorkIndex;

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

            if (gameStateController != null && option.timeToPassSeconds > 0f)
            {
                gameStateController.stateTimer += option.timeToPassSeconds;
            }

            if (playerController != null && option.rewardCash > 0)
            {
                playerController.Cash += option.rewardCash;
            }

            Debug.Log(
                $"[Work] index={index} time+={option.timeToPassSeconds:F1}s reward+={option.rewardCash} cash={playerController?.Cash ?? 0}");
        }
    }
}
