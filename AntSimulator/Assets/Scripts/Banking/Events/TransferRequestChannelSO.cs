using System;
using UnityEngine;
using Banking.Contracts;
using Banking.Core;

namespace Banking.Events
{
    [CreateAssetMenu(
        menuName = "Scriptable Objects/TransferRequestChannel",
        fileName = "TransferRequestChannel")]
    public class TransferRequestChannelSO : ScriptableObject, ITransferRequestSource
    {
        public event Action<TransferRequest> OnRaised;

        public void Raise(TransferRequest request)
        {
            OnRaised?.Invoke(request);
        }
    }
}
