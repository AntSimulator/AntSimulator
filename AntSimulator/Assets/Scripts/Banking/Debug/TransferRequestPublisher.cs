using UnityEngine;
using Banking.Core;
using Banking.Events;

namespace Banking.Debugging
{
    public class TransferRequestPublisher : MonoBehaviour
    {
        [SerializeField] private TransferRequestChannelSO requestChannel;

        [Header("Debug Input")]
        [SerializeField] private string toAccount = "0000000000000000";
        [SerializeField] private long amount = 1000;
        [SerializeField] private string memo = "debug";

        [ContextMenu("Debug Raise Transfer Request")]
        public void DebugRaise()
        {
            if (requestChannel == null)
            {
                Debug.LogError("[Transfer] Request channel is not assigned.");
                return;
            }

            var request = TransferRequestFactory.Create(
                toAccount,
                amount.ToString(),
                memo);

            requestChannel.Raise(request);

            Debug.Log(
                $"[Transfer] Request Raised: correlationId={request.correlationId}, to={request.toAccount}, amount={request.amount}");
        }
    }
}
