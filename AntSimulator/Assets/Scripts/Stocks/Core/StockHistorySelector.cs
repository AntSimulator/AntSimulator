using System;
using Stocks.Models;

namespace Stocks.Core
{
    public static class StockHistorySelector
    {
        public static StockHistory10D SelectByCodeOrFirst(StockHistoryDatabase db, string stockCode)
        {
            if (db?.stocks == null || db.stocks.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(stockCode))
            {
                return db.stocks[0];
            }

            for (var i = 0; i < db.stocks.Count; i++)
            {
                var candidate = db.stocks[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.code, stockCode, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
