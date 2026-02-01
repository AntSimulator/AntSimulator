using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stocks.Models
{
    [Serializable]
    public class StockSeedDatabase
    {
        public List<StockSeedItem> stocks = new();
    }
}