using UnityEngine;
using Player.Runtime;

namespace Player.UI
{
    public class HPUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;

        private void OnEnable()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
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
