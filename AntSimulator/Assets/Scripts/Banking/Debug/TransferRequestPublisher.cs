using System;
using UnityEngine;
using Banking.Contracts;
using Banking.Events;

namespace AntSimulator.Banking.Debugging
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

            var request = new TransferRequest
            {
                fromAccount = "",
                toAccount = toAccount,
                amount = amount,
                memo = memo,
                correlationId = Guid.NewGuid().ToString("N")
            };

            requestChannel.Raise(request);

            Debug.Log(
                $"[Transfer] Request Raised: correlationId={request.correlationId}, to={request.toAccount}, amount={request.amount}");
        }
    }
}