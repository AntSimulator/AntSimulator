using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Banking.Contracts;
using Banking.Events;
using Player.Runtime;

namespace Expenses
{
    public class ExpenseManager : MonoBehaviour
    {
        [SerializeField] private GameStateController gameStateController;
        [SerializeField] private List<ExpenseDefinitionSO> expenseDefinitions;
        [SerializeField] private ExpenseResultChannelSO resultChannel;
        [SerializeField] private TransferRequestChannelSO transferRequestChannel;
        [SerializeField] private TransferResultChannelSO transferResultChannel;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GameObject MissedPopup;
        
        private readonly Dictionary<string, ExpenseRuntime> runtimeByKey = new();
        private readonly Dictionary<string, ExpensePaymentCopy> paymentCopyByKey = new();
        private readonly Dictionary<string, Queue<string>> pendingExpenseKeysByAccount = new();
        private readonly Dictionary<string, TransferRequest> requestsByCorrelationId = new();
        private readonly Dictionary<string, TransferResult> bufferedResultsByCorrelationId = new();

        private int currentDay = -1;

        private void OnEnable()
        {
            BuildRuntimeLedger();

            if (gameStateController != null)
            {
                gameStateController.OnDayStarted += HandleDayStarted;
            }

            if (transferRequestChannel != null)
            {
                transferRequestChannel.OnRaised += HandleTransferRequest;
            }

            if (transferResultChannel != null)
            {
                transferResultChannel.OnRaised += HandleTransferResult;
            }
        }

        private void OnDisable()
        {
            if (gameStateController != null)
            {
                gameStateController.OnDayStarted -= HandleDayStarted;
            }

            if (transferRequestChannel != null)
            {
                transferRequestChannel.OnRaised -= HandleTransferRequest;
            }

            if (transferResultChannel != null)
            {
                transferResultChannel.OnRaised -= HandleTransferResult;
            }

            requestsByCorrelationId.Clear();
            bufferedResultsByCorrelationId.Clear();
        }

        private void BuildRuntimeLedger()
        {
            runtimeByKey.Clear();
            paymentCopyByKey.Clear();
            pendingExpenseKeysByAccount.Clear();
            requestsByCorrelationId.Clear();
            bufferedResultsByCorrelationId.Clear();

            if (expenseDefinitions == null) return;

            var sorted = new List<ExpenseDefinitionSO>(expenseDefinitions);
            sorted.Sort((a, b) =>
            {
                if (ReferenceEquals(a, b)) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return a.dueDay.CompareTo(b.dueDay);
            });

            int sequence = 0;
            foreach (var def in sorted)
            {
                if (def == null) continue;

                string expenseKey = BuildExpenseKey(def, sequence++);
                var runtime = new ExpenseRuntime(expenseKey, def);
                runtimeByKey[expenseKey] = runtime;

                paymentCopyByKey[expenseKey] = new ExpensePaymentCopy
                {
                    accountNumber = runtime.accountNumber,
                    remainingAmount = runtime.remainingAmount
                };

                if (!pendingExpenseKeysByAccount.TryGetValue(runtime.accountNumber, out var queue))
                {
                    queue = new Queue<string>();
                    pendingExpenseKeysByAccount[runtime.accountNumber] = queue;
                }

                queue.Enqueue(expenseKey);
            }
        }

        private void HandleDayStarted(int day)
        {
            currentDay = day;
            ResolveDueExpenses(day);
        }

        public void ResolveDueExpenses()
        {
            if (currentDay < 0)
            {
                currentDay = gameStateController != null ? gameStateController.currentDay : 1;
            }

            ResolveDueExpenses(currentDay);
        }

        private void ResolveDueExpenses(int day)
        {
            foreach (var runtime in runtimeByKey.Values)
            {
                if (runtime.resultPublished) continue;

                if (runtime.remainingAmount <= 0)
                {
                    PublishResult(new ExpenseResult
                    {
                        expenseId = runtime.expenseId,
                        displayName = runtime.displayName,
                        amount = runtime.totalAmount,
                        success = true,
                        reason = ExpenseFailReason.None
                    });
                    
                    if (MissedPopup != null)
                    {
                        var tmp = MissedPopup.GetComponentInChildren<TMP_Text>();
                        if (tmp != null)
                            tmp.text = $"{runtime.displayName}를 제때 납부했습니다!";
                        
                        MissedPopup.SetActive(true);
                    }

                    Debug.Log($"[Expense] Settled expenseId={runtime.expenseId} dueDay={runtime.dueDay} day={day}");
                }
                
                else if (runtime.dueDay > day) continue;
                else
                {
                    PublishResult(new ExpenseResult
                    {
                        expenseId = runtime.expenseId,
                        displayName = runtime.displayName,
                        amount = runtime.totalAmount,
                        success = false,
                        reason = ExpenseFailReason.MissedDeadline
                    });
                    
                    playerController.SubtractCash(runtime.remainingAmount * 2);

                    RemoveFromPendingIndexes(runtime.expenseKey, runtime.accountNumber);
                    Debug.LogWarning(
                        $"[Expense] Missed deadline expenseId={runtime.expenseId} dueDay={runtime.dueDay} day={day} remaining={runtime.remainingAmount}");

                    if (MissedPopup != null)
                    {
                        var tmp = MissedPopup.GetComponentInChildren<TMP_Text>();
                        if (tmp != null)
                            tmp.text = $"{runtime.displayName}을 놓쳤습니다...! 벌금 : {runtime.remainingAmount * 2}";
                        
                        MissedPopup.SetActive(true);
                    }
                }

                runtime.resultPublished = true;
            }
        }

        private void HandleTransferRequest(TransferRequest request)
        {
            if (string.IsNullOrEmpty(request.correlationId))
            {
                return;
            }

            requestsByCorrelationId[request.correlationId] = request;

            if (bufferedResultsByCorrelationId.TryGetValue(request.correlationId, out var buffered))
            {
                bufferedResultsByCorrelationId.Remove(request.correlationId);
                ProcessTransferResult(request, buffered);
            }
        }

        private void HandleTransferResult(TransferResult result)
        {
            if (string.IsNullOrEmpty(result.correlationId))
            {
                return;
            }

            if (!requestsByCorrelationId.TryGetValue(result.correlationId, out var request))
            {
                bufferedResultsByCorrelationId[result.correlationId] = result;
                Debug.Log($"[Expense] Buffer transfer result correlationId={result.correlationId}");
                return;
            }

            ProcessTransferResult(request, result);
        }

        private void ProcessTransferResult(TransferRequest request, TransferResult result)
        {
            requestsByCorrelationId.Remove(result.correlationId);

            if (!result.success)
            {
                Debug.LogWarning(
                    $"[Expense] Transfer failed correlationId={result.correlationId} reason={result.reason} day={currentDay}");
                return;
            }

            ApplyPayment(request.toAccount, request.amount, result.correlationId);
        }

        private void ApplyPayment(string toAccount, long amount, string correlationId)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[Expense] Ignore non-positive amount correlationId={correlationId} amount={amount}");
                return;
            }

            string account = toAccount ?? string.Empty;
            if (!pendingExpenseKeysByAccount.TryGetValue(account, out var queue))
            {
                Debug.Log($"[Expense] No pending expense for account={account} correlationId={correlationId}");
                return;
            }

            while (queue.Count > 0)
            {
                string expenseKey = queue.Peek();

                if (!runtimeByKey.TryGetValue(expenseKey, out var runtime))
                {
                    queue.Dequeue();
                    continue;
                }

                if (runtime.resultPublished || runtime.remainingAmount <= 0 || !paymentCopyByKey.ContainsKey(expenseKey))
                {
                    queue.Dequeue();
                    continue;
                }

                runtime.remainingAmount -= amount;
                if (runtime.remainingAmount < 0)
                {
                    runtime.remainingAmount = 0;
                }

                if (runtime.remainingAmount <= 0)
                {
                    RemoveFromPendingIndexes(runtime.expenseKey, runtime.accountNumber);
                    Debug.Log(
                        $"[Expense] Paid expenseId={runtime.expenseId} account={account} amount={amount} correlationId={correlationId}");
                }
                else
                {
                    paymentCopyByKey[expenseKey] = new ExpensePaymentCopy
                    {
                        accountNumber = runtime.accountNumber,
                        remainingAmount = runtime.remainingAmount
                    };

                    Debug.Log(
                        $"[Expense] Partial payment expenseId={runtime.expenseId} remaining={runtime.remainingAmount} amount={amount} correlationId={correlationId}");
                }

                return;
            }

            pendingExpenseKeysByAccount.Remove(account);
            Debug.Log($"[Expense] No active expense left for account={account} correlationId={correlationId}");
        }

        private void RemoveFromPendingIndexes(string expenseKey, string accountNumber)
        {
            paymentCopyByKey.Remove(expenseKey);

            if (!pendingExpenseKeysByAccount.TryGetValue(accountNumber, out var queue))
            {
                return;
            }

            while (queue.Count > 0)
            {
                string key = queue.Peek();
                if (!runtimeByKey.TryGetValue(key, out var runtime) || runtime.resultPublished || runtime.remainingAmount <= 0 || !paymentCopyByKey.ContainsKey(key))
                {
                    queue.Dequeue();
                    continue;
                }

                break;
            }

            if (queue.Count == 0)
            {
                pendingExpenseKeysByAccount.Remove(accountNumber);
            }
        }

        private void PublishResult(ExpenseResult result)
        {
            if (resultChannel == null)
            {
                Debug.LogError($"[Expense] ResultChannel is missing. expenseId={result.expenseId}");
                return;
            }

            resultChannel.Raise(result);
        }

        private static string BuildExpenseKey(ExpenseDefinitionSO def, int sequence)
        {
            return $"{def.expenseId}_{def.dueDay}_{sequence}";
        }

        private class ExpenseRuntime
        {
            public readonly string expenseKey;
            public readonly string expenseId;
            public readonly string displayName;
            public readonly int dueDay;
            public readonly long totalAmount;
            public readonly string accountNumber;

            public long remainingAmount;
            public bool resultPublished;

            public ExpenseRuntime(string key, ExpenseDefinitionSO def)
            {
                expenseKey = key;
                expenseId = def.expenseId;
                displayName = def.displayName;
                dueDay = def.dueDay;
                totalAmount = def.amount;
                accountNumber = def.accountNumber ?? string.Empty;
                remainingAmount = def.amount;
                resultPublished = false;
            }
        }

        private struct ExpensePaymentCopy
        {
            public string accountNumber;
            public long remainingAmount;
        }
    }
}
