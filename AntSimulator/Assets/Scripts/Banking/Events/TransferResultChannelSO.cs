using System;
using UnityEngine;
using Banking.Contracts;
using Banking.Core;

namespace Banking.Events
{
    [CreateAssetMenu(
        menuName = "Scriptable Objects/TransferResultChannel",
        fileName = "TransferResultChannel")]
    public class TransferResultChannelSO : ScriptableObject, ITransferResultPublisher
    {
        public event Action<TransferResult> OnRaised;

        public void Raise(TransferResult result)
        {
            OnRaised?.Invoke(result);
        }
    }
}
