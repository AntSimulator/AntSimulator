using System;
using System.Collections.Generic;

namespace Stocks.Models
{
    [Serializable]
    public class Candle { public int tick; public float price; public long volume; }

    [Serializable]
    public class StockHistory10D
    {
        public string code;
        public string name;
        public List<Candle> candles;
    }

    [Serializable]
    public class StockHistoryDatabase
    {
        public List<StockHistory10D> stocks = new();
    }
}
