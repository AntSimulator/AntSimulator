using UnityEngine;
using TMPro;
using Banking.Core;
using Banking.Events;

namespace Banking.UI
{
    public class OnlineBankingTransferUI : MonoBehaviour
    {
        [SerializeField] private TransferRequestChannelSO requestChannel;

        [Header("UI")]
        [SerializeField] private TMP_InputField toAccountInput;
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private TMP_InputField memoInput;

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

            var toAccount = toAccountInput.text;
            var amountStr = amountInput.text;
            var memo = memoInput != null ? memoInput.text : string.Empty;
            var req = TransferRequestFactory.Create(toAccount, amountStr, memo);

            requestChannel.Raise(req);
            Debug.Log($"[TransferUI] Raised from UI: correlationId={req.correlationId}, to={req.toAccount}, amount={req.amount}");
        }
    }
}
