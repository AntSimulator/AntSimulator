using System;

namespace Stocks.UI
{
    public static class StockSelectionEvents
    {
        public static event Action<string, string> OnStockSelected;

        public static void RaiseSelected(string stockCode, string stockName)
        {
            if (string.IsNullOrWhiteSpace(stockCode))
            {
                return;
            }

            OnStockSelected?.Invoke(stockCode, stockName ?? string.Empty);
        }
    }
}
