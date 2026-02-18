using UnityEngine;

namespace Expenses
{
    [CreateAssetMenu(fileName = "ExpenseDefinition", menuName = "Scriptable Objects/ExpenseDefinition")]
    public class ExpenseDefinitionSO : ScriptableObject
    {
        public string expenseId;
        public string displayName;
        public int dueDay;
        public long amount;
    }
}
