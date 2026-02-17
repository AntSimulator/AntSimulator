using System;
using System.Collections.Generic;
using Player.Models;

namespace Player.Core
{
    public sealed class PlayerTradingEngine
    {
        private readonly PlayerState playerState;
        private readonly List<TradeStockState> stocks = new();
        private int selectedIndex;

        public PlayerTradingEngine(PlayerState playerState)
        {
            this.playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        }

        public IReadOnlyList<TradeStockState> Stocks => stocks;

        public TradeStockState CurrentStock
        {
            get
            {
                if (stocks.Count == 0)
                {
                    return null;
                }

                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }

                if (selectedIndex >= stocks.Count)
                {
                    selectedIndex = stocks.Count - 1;
                }

                return stocks[selectedIndex];
            }
        }

        public void ReplaceStocks(IEnumerable<TradeStockState> newStocks)
        {
            stocks.Clear();
            if (newStocks != null)
            {
                foreach (var stock in newStocks)
                {
                    if (stock == null || string.IsNullOrWhiteSpace(stock.stockId))
                    {
                        continue;
                    }

                    stocks.Add(stock);
                }
            }

            if (stocks.Count == 0)
            {
                selectedIndex = 0;
                return;
            }

            if (selectedIndex >= stocks.Count)
            {
                selectedIndex = stocks.Count - 1;
            }
        }

        public void SelectByIndex(int index)
        {
            if (stocks.Count == 0)
            {
                selectedIndex = 0;
                return;
            }

            if (index < 0)
            {
                selectedIndex = 0;
                return;
            }

            if (index >= stocks.Count)
            {
                selectedIndex = stocks.Count - 1;
                return;
            }

            selectedIndex = index;
        }

        public bool SelectStockById(string stockId)
        {
            if (string.IsNullOrWhiteSpace(stockId) || stocks.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < stocks.Count; i++)
            {
                if (string.Equals(stocks[i].stockId, stockId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    return true;
                }
            }

            return false;
        }

        public TradeActionResult Buy(int qty)
        {
            var current = CurrentStock;
            if (current == null)
            {
                return TradeActionResult.Fail("현재 선택된 종목이 없습니다.");
            }

            if (!playerState.TryBuy(current.stockId, qty, current.currentPrice, out var reason))
            {
                return TradeActionResult.Fail(reason);
            }

            return TradeActionResult.Ok();
        }

        public TradeActionResult Sell(int qty)
        {
            var current = CurrentStock;
            if (current == null)
            {
                return TradeActionResult.Fail("현재 선택된 종목이 없습니다.");
            }

            if (!playerState.TrySell(current.stockId, qty, current.currentPrice, out var reason))
            {
                return TradeActionResult.Fail(reason);
            }

            return TradeActionResult.Ok();
        }

        public bool PriceUp(int step)
        {
            var current = CurrentStock;
            if (current == null)
            {
                return false;
            }

            current.SetPrice(current.currentPrice + step);
            return true;
        }

        public bool PriceDown(int step)
        {
            var current = CurrentStock;
            if (current == null)
            {
                return false;
            }

            current.SetPrice(Math.Max(1, current.currentPrice - step));
            return true;
        }

        public int GetCurrentQuantity()
        {
            var current = CurrentStock;
            return current == null ? 0 : playerState.GetQuantity(current.stockId);
        }
    }
}
