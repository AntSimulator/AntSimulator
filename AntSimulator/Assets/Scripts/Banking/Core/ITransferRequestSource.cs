using System;
using Banking.Contracts;

namespace Banking.Core
{
    public interface ITransferRequestSource
    {
        event Action<TransferRequest> OnRaised;
    }
}
