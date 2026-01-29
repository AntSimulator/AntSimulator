using System;

namespace Player
{
    [Serializable]
    public class Holding
    {
        public string stockId;
        public int quantity;
        public float avgBuyPrice;

        public Holding(string stockId)
        {
            this.stockId = stockId;
            quantity = 0;
            avgBuyPrice = 0f;
        }
    }
}