using System;

namespace Expenses
{
    public enum ExpenseFailReason { None, InsufficientFunds }

    [Serializable]
    public struct ExpenseResult
    {
        public string expenseId;
        public string displayName;
        public long amount;
        public bool success;
        public ExpenseFailReason reason;
    }
}
