using System;
using Banking.Contracts;

namespace Banking.Core
{
    public static class TransferRequestFactory
    {
        public static TransferRequest Create(string toAccount, string amountText, string memo, string fromAccount = "")
        {
            long amount = 0;
            if (!long.TryParse(amountText, out amount))
            {
                amount = 0;
            }

            return new TransferRequest
            {
                fromAccount = fromAccount ?? string.Empty,
                toAccount = toAccount ?? string.Empty,
                amount = amount,
                memo = memo ?? string.Empty,
                correlationId = Guid.NewGuid().ToString("N")
            };
        }
    }
}
