using System.Collections.Generic;
using UnityEngine;
using Player.Runtime;

namespace Expenses
{
    public class ExpenseManager : MonoBehaviour
    {
        [SerializeField] private GameStateController gameStateController;
        [SerializeField] private List<ExpenseDefinitionSO> expenseDefinitions;
        [SerializeField] private ExpenseResultChannelSO resultChannel;
        [SerializeField] private PlayerController playerController;

        private void OnEnable()
        {
            gameStateController.OnDayStarted += HandleDayStarted;
        }

        private void OnDisable()
        {
            gameStateController.OnDayStarted -= HandleDayStarted;
        }

        private void HandleDayStarted(int day)
        {
            foreach (var def in expenseDefinitions)
            {
                if (def.dueDay != day) continue;
                ProcessExpense(def);
            }
        }

        private void ProcessExpense(ExpenseDefinitionSO def)
        {
            long cash = playerController.Cash;
            bool canPay = cash >= def.amount;

            if (canPay)
            {
                playerController.Cash -= def.amount;
                resultChannel.Raise(new ExpenseResult
                {
                    expenseId = def.expenseId,
                    displayName = def.displayName,
                    amount = def.amount,
                    success = true,
                    reason = ExpenseFailReason.None
                });
            }
            else
            {
                resultChannel.Raise(new ExpenseResult
                {
                    expenseId = def.expenseId,
                    displayName = def.displayName,
                    amount = def.amount,
                    success = false,
                    reason = ExpenseFailReason.InsufficientFunds
                });
            }
        }
    }
}
