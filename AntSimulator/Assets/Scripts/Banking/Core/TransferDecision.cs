using Banking.Contracts;

namespace Banking.Core
{
    public readonly struct TransferDecision
    {
        public bool ShouldApplyCash { get; }
        public long NextCash { get; }
        public TransferResult Result { get; }

        public TransferDecision(bool shouldApplyCash, long nextCash, TransferResult result)
        {
            ShouldApplyCash = shouldApplyCash;
            NextCash = nextCash;
            Result = result;
        }
    }
}
