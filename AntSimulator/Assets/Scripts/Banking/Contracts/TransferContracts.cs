using System;

namespace Banking.Contracts
{
    /// <summary>
    /// 이체 결과 실패 사유(확장 대비용).
    /// 지금 단계에서는 Result를 사용하지 않아도, 계약은 미리 두는 편이 안전합니다.
    /// </summary>
    public enum TransferFailReason
    {
        None = 0,
        InvalidAccount,
        InvalidAmount,
        InsufficientFunds,
        SystemError
    }

    /// <summary>
    /// 이체 요청 계약(Contract).
    /// UI/로직/테스트 모두가 공유하는 표준 데이터.
    /// </summary>
    [Serializable]
    public struct TransferRequest
    {
        public string fromAccount;     // 아직 없으면 빈 문자열 허용
        public string toAccount;       // 목표 계좌(예: 16자리)
        public long amount;            // 금액(원 단위; long 권장)
        public string memo;            // 선택
        public string correlationId;   // 요청-응답 매칭용 GUID 등
    }

    /// <summary>
    /// (선택) 이체 결과 계약. 향후 결과 채널을 붙일 때 사용.
    /// </summary>
    [Serializable]
    public struct TransferResult
    {
        public string correlationId;
        public bool success;
        public TransferFailReason reason;
        public string message;
    }
}