using TMPro;
using UnityEngine;
using Player.Runtime;

namespace Player.UI
{
    public class HPUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private TMP_Text hpText;

        [Header("Display")]
        [SerializeField] private string hpTextFormat = "HP: {0}/{1}";

        private void OnEnable()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.OnHpChanged += HandleHpChanged;
                RefreshHpText();
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnHpChanged -= HandleHpChanged;
            }
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            UpdateHpText(currentHp, maxHp);
        }

        private void RefreshHpText()
        {
            if (playerController == null)
            {
                return;
            }

            UpdateHpText(playerController.CurrentHp, playerController.MaxHp);
        }

        private void UpdateHpText(int currentHp, int maxHp)
        {
            if (hpText == null)
            {
                return;
            }

            hpText.text = string.Format(hpTextFormat, currentHp, maxHp);
        }

        public void AddHp(int amount)
        {
            playerController?.AddHp(amount);
        }

        public void DecreaseHp(int amount)
        {
            playerController?.DecreaseHp(amount);
        }

        public void SetHp(int value)
        {
            playerController?.SetHp(value);
        }
    }
}
