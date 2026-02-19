using System;

namespace Player.Core
{
    [Serializable]
    public sealed class TradeStockState
    {
        public string stockId;
        public int currentPrice;

        public TradeStockState(string stockId, int currentPrice)
        {
            this.stockId = string.IsNullOrWhiteSpace(stockId) ? "UNKNOWN" : stockId;
            this.currentPrice = Math.Max(1, currentPrice);
        }

        public void SetPrice(int price)
        {
            currentPrice = Math.Max(1, price);
        }
    }
}
