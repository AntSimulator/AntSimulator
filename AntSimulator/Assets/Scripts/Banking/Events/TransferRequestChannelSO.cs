using System;
using UnityEngine;
using Banking.Contracts;

namespace Banking.Events
{
    [CreateAssetMenu(
        menuName = "AntSimulator/Events/Banking/Transfer Request Channel",
        fileName = "TransferRequestChannel")]
    public class TransferRequestChannelSO : ScriptableObject
    {
        public event Action<TransferRequest> OnRaised;

        public void Raise(TransferRequest request)
        {
            OnRaised?.Invoke(request);
        }
    }
}