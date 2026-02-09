using System;
using System.Collections.Generic;

namespace Stocks.Models
{
    [Serializable]
    public class StockSeedDatabase
    {
        public long currentBalance = 100000;
        public List<StockSeedItem> stocks = new();
    }
}
