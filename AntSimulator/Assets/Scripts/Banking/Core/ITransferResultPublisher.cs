using Banking.Contracts;

namespace Banking.Core
{
    public interface ITransferResultPublisher
    {
        void Raise(TransferResult result);
    }
}
