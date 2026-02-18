using System;
using UnityEngine;

namespace Expenses
{
    [CreateAssetMenu(menuName = "Scriptable Objects/ExpenseResultChannel", fileName = "ExpenseResultChannel")]
    public class ExpenseResultChannelSO : ScriptableObject
    {
        public event Action<ExpenseResult> OnRaised;

        public void Raise(ExpenseResult result) => OnRaised?.Invoke(result);
    }
}
