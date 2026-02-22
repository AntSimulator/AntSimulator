using TMPro;
using UnityEngine;

namespace Expenses
{
    public class ExpenseStickyNoteUI : MonoBehaviour
    {
        [Header("Defs (ScriptableObject)")]
        public ExpenseDefinitionSO rent;
        public ExpenseDefinitionSO utility;

        [Header("UI (choose one style)")]
        [Tooltip("한 텍스트에 렌트/공과금 같이 쓰고 싶으면 여기에 연결")]
        public TMP_Text combinedText;

        [Tooltip("렌트/공과금 텍스트를 따로 쓰고 싶으면 아래 두 개 연결")]
        public TMP_Text rentText;
        public TMP_Text utilityText;

        [Header("Format")]
        public bool showDueDay = true;
        public bool showAccountNumber = true;
        public bool showAmount = true;

        void OnEnable()
        {
            Refresh();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // 에디터에서 값 바꿀 때 바로 갱신되게(플레이 중 아니어도)
            if (!Application.isPlaying) Refresh();
        }
#endif

        public void Refresh()
        {
            // 1) combinedText 우선
            if (combinedText != null)
            {
                combinedText.text =
                    BuildLine(rent, prefix: "렌트비") + "\n\n" +
                    BuildLine(utility, prefix: "공과금");
            }

            // 2) 분리 텍스트도 지원
            if (rentText != null) rentText.text = BuildLine(rent, prefix: "렌트비");
            if (utilityText != null) utilityText.text = BuildLine(utility, prefix: "공과금");
        }

        string BuildLine(ExpenseDefinitionSO def, string prefix)
        {
            if (def == null) return $"{prefix}: (없음)";

            // 금액 표시 (천단위 콤마)
            string amountStr = showAmount ? $"{def.amount:n0}원" : "";

            // dueDay
            string dueStr = showDueDay ? $"{def.dueDay}" : "";

            // 계좌
            string accStr = showAccountNumber ? $"계좌번호 : {def.accountNumber}" : "";

            // 조합 (필요한 것만)
            // 예: "렌트비 D3 / 230,000원\n1234-...."
            string header = prefix;
            if (!string.IsNullOrEmpty(dueStr) || !string.IsNullOrEmpty(amountStr))
            {
                header += " ";
                if (!string.IsNullOrEmpty(dueStr)) header += dueStr;
                if (!string.IsNullOrEmpty(dueStr) && !string.IsNullOrEmpty(amountStr)) header += " / ";
                if (!string.IsNullOrEmpty(amountStr)) header += amountStr;
            }

            if (!string.IsNullOrWhiteSpace(accStr))
                return header + "\n" + accStr;

            return header;
        }
    }
}