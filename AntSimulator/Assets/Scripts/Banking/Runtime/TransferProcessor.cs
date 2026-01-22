using System;
using System.Reflection;
using UnityEngine;
using Banking.Contracts;
using Banking.Events;

namespace Banking.Runtime
{
    /// <summary>
    /// TransferRequest를 구독하고, 최소 검증 후 PlayerState의 현금을 차감한다.
    /// PlayerState 구현(필드/프로퍼티명)이 다를 수 있으므로 cashMemberName으로 지정한다.
    /// </summary>
    public class TransferProcessor : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private TransferRequestChannelSO requestChannel;

        // 결과 채널은 다음 커밋에서 UI 피드백용으로 붙이는 것을 권장.
        // [SerializeField] private TransferResultChannelSO resultChannel;

        [Header("Player Money Target")]
        [Tooltip("현금을 보관하는 컴포넌트(대개 PlayerState).")]
        [SerializeField] private Component playerMoneyComponent;

        [Tooltip("playerMoneyComponent 안에서 현금을 나타내는 필드/프로퍼티 이름. 예: cash, Cash, money, CurrentCash 등")]
        [SerializeField] private string cashMemberName = "cash";

        [Header("Validation")]
        [SerializeField] private bool enforceAccountLength16 = true;
        [SerializeField] private int accountLength = 16;

        private void OnEnable()
        {
            if (requestChannel != null)
                requestChannel.OnRaised += HandleTransferRequest;
        }

        private void OnDisable()
        {
            if (requestChannel != null)
                requestChannel.OnRaised -= HandleTransferRequest;
        }

        private void HandleTransferRequest(TransferRequest req)
        {
            // 1) 기본 검증
            if (enforceAccountLength16)
            {
                var toAcc = req.toAccount ?? string.Empty;
                if (toAcc.Length != accountLength)
                {
                    Debug.LogWarning($"[Transfer] Reject: InvalidAccount (len={toAcc.Length}) correlationId={req.correlationId}");
                    return;
                }
            }

            if (req.amount <= 0)
            {
                Debug.LogWarning($"[Transfer] Reject: InvalidAmount amount={req.amount} correlationId={req.correlationId}");
                return;
            }

            // 2) 플레이어 현금 조회
            if (playerMoneyComponent == null)
            {
                Debug.LogError("[Transfer] playerMoneyComponent is not assigned.");
                return;
            }

            if (!TryGetCash(playerMoneyComponent, cashMemberName, out long currentCash))
            {
                Debug.LogError($"[Transfer] Cannot read cash member '{cashMemberName}' from {playerMoneyComponent.GetType().Name}.");
                return;
            }

            // 3) 잔액 검증
            if (currentCash < req.amount)
            {
                Debug.LogWarning($"[Transfer] Reject: InsufficientFunds cash={currentCash} amount={req.amount} correlationId={req.correlationId}");
                return;
            }

            // 4) 차감 반영
            long newCash = currentCash - req.amount;
            if (!TrySetCash(playerMoneyComponent, cashMemberName, newCash))
            {
                Debug.LogError($"[Transfer] Cannot write cash member '{cashMemberName}' to {playerMoneyComponent.GetType().Name}.");
                return;
            }

            Debug.Log($"[Transfer] Approved: cash {currentCash} -> {newCash}, to={req.toAccount}, amount={req.amount}, correlationId={req.correlationId}");

            // (다음 단계) resultChannel.Raise(...)로 UI 피드백까지 처리
        }

        private static bool TryGetCash(Component target, string memberName, out long cash)
        {
            cash = 0;
            if (target == null || string.IsNullOrWhiteSpace(memberName)) return false;

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Field 우선
            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                var val = field.GetValue(target);
                return TryConvertToLong(val, out cash);
            }

            // Property 다음
            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanRead)
            {
                var val = prop.GetValue(target);
                return TryConvertToLong(val, out cash);
            }

            return false;
        }

        private static bool TrySetCash(Component target, string memberName, long newValue)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName)) return false;

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var field = type.GetField(memberName, flags);
            if (field != null && !field.IsInitOnly)
            {
                return TryAssignNumeric(field.FieldType, newValue, v => field.SetValue(target, v));
            }

            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanWrite)
            {
                return TryAssignNumeric(prop.PropertyType, newValue, v => prop.SetValue(target, v));
            }

            return false;
        }

        private static bool TryConvertToLong(object val, out long result)
        {
            result = 0;
            if (val == null) return false;

            try
            {
                switch (val)
                {
                    case long l: result = l; return true;
                    case int i: result = i; return true;
                    case short s: result = s; return true;
                    case byte b: result = b; return true;
                    case float f: result = (long)f; return true;
                    case double d: result = (long)d; return true;
                    case decimal m: result = (long)m; return true;
                    case string str when long.TryParse(str, out var parsed): result = parsed; return true;
                    default:
                        // Convert 시도
                        result = Convert.ToInt64(val);
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryAssignNumeric(Type memberType, long newValue, Action<object> assign)
        {
            try
            {
                if (memberType == typeof(long)) { assign(newValue); return true; }
                if (memberType == typeof(int)) { assign((int)newValue); return true; }
                if (memberType == typeof(short)) { assign((short)newValue); return true; }
                if (memberType == typeof(byte)) { assign((byte)newValue); return true; }
                if (memberType == typeof(float)) { assign((float)newValue); return true; }
                if (memberType == typeof(double)) { assign((double)newValue); return true; }
                if (memberType == typeof(decimal)) { assign((decimal)newValue); return true; }

                // 문자열로 보관하는 특이 케이스
                if (memberType == typeof(string)) { assign(newValue.ToString()); return true; }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
