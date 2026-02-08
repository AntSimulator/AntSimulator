using System;

namespace Stocks.Models
{
    [Serializable]
    public class StockSeedItem
    {
        public string code;       // 예: "005930"
        public string name;       // 예: "삼성전자"
        public string iconColor;  // 예: "#FF3B30"
        public int amount;        // 초기 보유 수량
        public int price;         // 초기 가격
    }
}
