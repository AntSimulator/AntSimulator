using System;
using UnityEngine;

namespace Player.Models
{
    [Serializable]
    public class PlayerHp
    {
        private int currentHp;
        private readonly int maxHp;

        public event Action<int, int> OnHpChanged;

        public int CurrentHp => currentHp;

        public int MaxHp => maxHp;

        public PlayerHp(int startHp, int maxHp)
        {
            this.maxHp = Mathf.Max(1, maxHp);
            currentHp = Mathf.Clamp(startHp, 0, this.maxHp);
        }

        public void AddHp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetHp(currentHp + amount);
        }

        public void DecreaseHp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetHp(currentHp - amount);
        }

        public void SetHp(int value)
        {
            var nextHp = Mathf.Clamp(value, 0, maxHp);
            if (nextHp == currentHp)
            {
                return;
            }

            currentHp = nextHp;
            OnHpChanged?.Invoke(currentHp, maxHp);
        }
    }
}
