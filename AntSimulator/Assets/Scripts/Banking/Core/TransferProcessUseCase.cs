using Banking.Contracts;

namespace Banking.Core
{
    public static class TransferProcessUseCase
    {
        public static TransferDecision Evaluate(
            TransferRequest request,
            long currentCash,
            bool enforceAccountLength,
            int accountLength)
        {
            if (enforceAccountLength)
            {
                var toAcc = request.toAccount ?? string.Empty;
                if (toAcc.Length != accountLength)
                {
                    return Reject(request, TransferFailReason.InvalidAccount, "Invalid account length.");
                }
            }

            if (request.amount <= 0)
            {
                return Reject(request, TransferFailReason.InvalidAmount, "Amount must be greater than zero.");
            }

            if (currentCash < request.amount)
            {
                return Reject(request, TransferFailReason.InsufficientFunds, "Insufficient funds.");
            }

            var nextCash = currentCash - request.amount;
            var success = new TransferResult
            {
                correlationId = request.correlationId ?? string.Empty,
                success = true,
                reason = TransferFailReason.None,
                message = "Approved"
            };

            return new TransferDecision(true, nextCash, success);
        }

        private static TransferDecision Reject(TransferRequest request, TransferFailReason reason, string message)
        {
            var result = new TransferResult
            {
                correlationId = request.correlationId ?? string.Empty,
                success = false,
                reason = reason,
                message = message ?? string.Empty
            };

            return new TransferDecision(false, 0, result);
        }
    }
}
