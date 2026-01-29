using System;
using UnityEngine;

namespace Stocks.Models
{
    [Serializable]
    public class StockSeedItem
    {
        public string code;       // 예: "005930"
        public string name;       // 예: "삼성전자"
        public string iconColor;  // 예: "#FF3B30"
    }
}