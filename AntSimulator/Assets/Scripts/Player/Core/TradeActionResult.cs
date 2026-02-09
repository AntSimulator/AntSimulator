namespace Player.Core
{
    public readonly struct TradeActionResult
    {
        public bool Success { get; }
        public string Reason { get; }

        public TradeActionResult(bool success, string reason)
        {
            Success = success;
            Reason = reason ?? string.Empty;
        }

        public static TradeActionResult Ok()
        {
            return new TradeActionResult(true, string.Empty);
        }

        public static TradeActionResult Fail(string reason)
        {
            return new TradeActionResult(false, reason);
        }
    }
}
