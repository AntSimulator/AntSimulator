using System;
using UnityEngine;
using TMPro;
using Banking.Contracts;
using Banking.Events;

namespace Banking.UI
{
    public class OnlineBankingTransferUI : MonoBehaviour
    {
        [SerializeField] private TransferRequestChannelSO requestChannel;

        [Header("UI")]
        [SerializeField] private TMP_InputField toAccountInput;
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private TMP_InputField memoInput; // 선택

        public void OnClickTransfer()
        {
            if (requestChannel == null)
            {
                Debug.LogError("[TransferUI] Request channel is not assigned.");
                return;
            }

            if (toAccountInput == null)
            {
                Debug.LogError("[TransferUI] toAccountInput is not assigned.");
                return;
            }

            if (amountInput == null)
            {
                Debug.LogError("[TransferUI] amountInput is not assigned.");
                return;
            }

            string toAccount = toAccountInput.text;
            string amountStr = amountInput.text;
            string memo = memoInput != null ? memoInput.text : "";

            if (!long.TryParse(amountStr, out long amount))
                amount = 0;

            var req = new TransferRequest
            {
                fromAccount = "",
                toAccount = toAccount ?? "",
                amount = amount,
                memo = memo ?? "",
                correlationId = Guid.NewGuid().ToString("N")
            };

            requestChannel.Raise(req);
            Debug.Log($"[TransferUI] Raised from UI: correlationId={req.correlationId}, to={req.toAccount}, amount={req.amount}");
        }
    }
}
