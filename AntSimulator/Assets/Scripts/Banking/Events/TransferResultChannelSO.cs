using System;
using UnityEngine;
using Banking.Contracts;

namespace Banking.Events
{
    [CreateAssetMenu(
        menuName = "AntSimulator/Events/Banking/Transfer Result Channel",
        fileName = "TransferResultChannel")]
    public class TransferResultChannelSO : ScriptableObject
    {
        public event Action<TransferResult> OnRaised;

        public void Raise(TransferResult result)
        {
            OnRaised?.Invoke(result);
        }
    }
}