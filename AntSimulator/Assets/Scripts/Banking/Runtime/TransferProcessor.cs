using UnityEngine;
using Banking.Contracts;
using Banking.Core;
using Banking.Events;
using Banking.UnityAdapter;

namespace Banking.Runtime
{
    /// <summary>
    /// 요청 채널을 구독하고 코어 UseCase로 이체 승인/거절을 판단한 뒤 현금을 반영한다.
    /// </summary>
    public class TransferProcessor : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private TransferRequestChannelSO requestChannel;
        [SerializeField] private TransferResultChannelSO resultChannel;

        [Header("Player Money Target")]
        [Tooltip("현금을 보관하는 컴포넌트(대개 PlayerController).")]
        [SerializeField] private Component playerMoneyComponent;

        [Tooltip("playerMoneyComponent 안에서 현금을 나타내는 필드/프로퍼티 이름. 예: cash, Cash, money, CurrentCash 등")]
        [SerializeField] private string cashMemberName = "cash";

        [Header("Validation")]
        [SerializeField] private bool enforceAccountLength16 = true;
        [SerializeField] private int accountLength = 16;

        private void OnEnable()
        {
            if (requestChannel != null)
            {
                requestChannel.OnRaised += HandleTransferRequest;
            }
        }

        private void OnDisable()
        {
            if (requestChannel != null)
            {
                requestChannel.OnRaised -= HandleTransferRequest;
            }
        }

        private void HandleTransferRequest(TransferRequest req)
        {
            var preCheck = TransferProcessUseCase.Evaluate(
                req,
                long.MaxValue,
                enforceAccountLength16,
                accountLength);

            if (!preCheck.Result.success)
            {
                if (preCheck.Result.reason == TransferFailReason.InvalidAccount)
                {
                    var toAcc = req.toAccount ?? string.Empty;
                    Debug.LogWarning($"[Transfer] Reject: InvalidAccount (len={toAcc.Length}) correlationId={req.correlationId}");
                }
                else if (preCheck.Result.reason == TransferFailReason.InvalidAmount)
                {
                    Debug.LogWarning($"[Transfer] Reject: InvalidAmount amount={req.amount} correlationId={req.correlationId}");
                }
                else
                {
                    Debug.LogWarning($"[Transfer] Reject: {preCheck.Result.reason} correlationId={req.correlationId}");
                }

                resultChannel?.Raise(preCheck.Result);
                return;
            }

            if (playerMoneyComponent == null)
            {
                Debug.LogError("[Transfer] playerMoneyComponent is not assigned.");
                return;
            }

            if (!ReflectionCashAccessor.TryGetCash(playerMoneyComponent, cashMemberName, out var currentCash))
            {
                Debug.LogError($"[Transfer] Cannot read cash member '{cashMemberName}' from {playerMoneyComponent.GetType().Name}.");
                return;
            }

            var decision = TransferProcessUseCase.Evaluate(
                req,
                currentCash,
                enforceAccountLength16,
                accountLength);

            if (!decision.Result.success)
            {
                Debug.LogWarning($"[Transfer] Reject: InsufficientFunds cash={currentCash} amount={req.amount} correlationId={req.correlationId}");
                resultChannel?.Raise(decision.Result);
                return;
            }

            if (!ReflectionCashAccessor.TrySetCash(playerMoneyComponent, cashMemberName, decision.NextCash))
            {
                Debug.LogError($"[Transfer] Cannot write cash member '{cashMemberName}' to {playerMoneyComponent.GetType().Name}.");
                return;
            }

            Debug.Log(
                $"[Transfer] Approved: cash {currentCash} -> {decision.NextCash}, " +
                $"to={req.toAccount}, amount={req.amount}, correlationId={req.correlationId}");

            resultChannel?.Raise(decision.Result);
        }
    }
}
