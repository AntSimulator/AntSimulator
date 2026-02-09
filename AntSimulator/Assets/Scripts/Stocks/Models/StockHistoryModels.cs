using System;
using System.Collections.Generic;
using UnityEngine.Experimental.GlobalIllumination;

namespace Stocks.Models
{
    [Serializable]
    public class Candle
    {
        public int tick;
        public float open;
        public float high;
        public float low;
        public float close;
        public long volume;
    }

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
