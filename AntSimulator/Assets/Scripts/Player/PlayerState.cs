using System;
using System.Collections.Generic;

[Serializable]
public class PlayerState
{
    public long cash;
    public Dictionary<string, Holding> holdings = new();

    public PlayerState(long startCash)
    {
        cash = startCash;
    }

    public bool TryBuy(string stockId, int qty, float price, out string reason)
    {
        reason = "";
        if (qty <= 0) { reason = "qty는 1 이상이어야 합니다."; return false; }
        if (price <= 0) { reason = "price가 0 이하입니다."; return false; }

        long cost = (long)Math.Round(qty * price);
        if (cash < cost) { reason = "현금 부족"; return false; }

        if (!holdings.TryGetValue(stockId, out var h))
        {
            h = new Holding(stockId);
            holdings.Add(stockId, h);
        }

        // 평균 매입가 갱신
        float prevTotal = h.quantity * h.avgBuyPrice;
        float newTotal = prevTotal + qty * price;

        h.quantity += qty;
        h.avgBuyPrice = h.quantity > 0 ? (newTotal / h.quantity) : 0f;

        cash -= cost;
        return true;
    }

    public bool TrySell(string stockId, int qty, float price, out string reason)
    {
        reason = "";
        if (qty <= 0) { reason = "qty는 1 이상이어야 합니다."; return false; }
        if (price <= 0) { reason = "price가 0 이하입니다."; return false; }

        if (!holdings.TryGetValue(stockId, out var h) || h.quantity <= 0)
        {
            reason = "보유 주식 없음";
            return false;
        }

        if (h.quantity < qty)
        {
            reason = "보유 수량 부족";
            return false;
        }

        long revenue = (long)Math.Round(qty * price);
        h.quantity -= qty;
        cash += revenue;

        // 수량이 0이면 평균 단가 초기화(선택)
        if (h.quantity == 0) h.avgBuyPrice = 0f;

        return true;
    }

    public int GetQuantity(string stockId)
    {
        return holdings.TryGetValue(stockId, out var h) ? h.quantity : 0;
    }
    //new
}
