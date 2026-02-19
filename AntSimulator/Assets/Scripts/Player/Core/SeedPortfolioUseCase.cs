using System;
using System.Collections.Generic;
using Player.Models;
using Stocks.Models;

namespace Player.Core
{
    public sealed class SeedPortfolioUseCase
    {
        public SeedApplyResult Apply(
            PlayerState playerState,
            IReadOnlyList<TradeStockState> currentStocks,
            StockSeedDatabase db,
            bool seedOverrideStocks,
            int seedDefaultPrice)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            if (db == null)
            {
                return SeedApplyResult.FromCurrent(currentStocks);
            }

            if (db.currentBalance >= 0)
            {
                playerState.SetCash(db.currentBalance);
            }

            playerState.ClearHoldings();

            if (db.stocks == null || db.stocks.Count == 0)
            {
                return SeedApplyResult.FromCurrent(currentStocks);
            }

            if (seedOverrideStocks)
            {
                var loaded = new List<TradeStockState>(db.stocks.Count);
                for (var i = 0; i < db.stocks.Count; i++)
                {
                    var seedItem = db.stocks[i];
                    var seedCode = ResolveSeedCode(seedItem, i);
                    var seedPrice = ResolveSeedPrice(seedItem, seedDefaultPrice);
                    loaded.Add(new TradeStockState(seedCode, seedPrice));

                    if (seedItem != null && seedItem.amount > 0)
                    {
                        playerState.SetHolding(seedCode, seedItem.amount, seedPrice);
                    }
                }

                return new SeedApplyResult(loaded);
            }

            var updated = new List<TradeStockState>();
            if (currentStocks != null)
            {
                for (var i = 0; i < currentStocks.Count; i++)
                {
                    var stock = currentStocks[i];
                    if (stock == null)
                    {
                        continue;
                    }

                    if (TryFindSeedItem(db, stock.stockId, out var seedItem))
                    {
                        var seedPrice = ResolveSeedPrice(seedItem, seedDefaultPrice);
                        stock.SetPrice(seedPrice);

                        if (seedItem.amount > 0)
                        {
                            playerState.SetHolding(stock.stockId, seedItem.amount, seedPrice);
                        }
                    }

                    updated.Add(stock);
                }
            }

            return new SeedApplyResult(updated);
        }

        private static int ResolveSeedPrice(StockSeedItem item, int defaultPrice)
        {
            if (item != null && item.price > 0)
            {
                return item.price;
            }

            return Math.Max(1, defaultPrice);
        }

        private static string ResolveSeedCode(StockSeedItem item, int index)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.code))
            {
                return item.code;
            }

            return $"SEED_{index + 1:000}";
        }

        private static bool TryFindSeedItem(StockSeedDatabase db, string stockId, out StockSeedItem found)
        {
            found = null;
            if (db?.stocks == null || db.stocks.Count == 0 || string.IsNullOrWhiteSpace(stockId))
            {
                return false;
            }

            for (var i = 0; i < db.stocks.Count; i++)
            {
                var seedItem = db.stocks[i];
                if (seedItem == null)
                {
                    continue;
                }

                if (string.Equals(seedItem.code, stockId, StringComparison.OrdinalIgnoreCase))
                {
                    found = seedItem;
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class SeedApplyResult
    {
        public List<TradeStockState> Stocks { get; }

        public SeedApplyResult(List<TradeStockState> stocks)
        {
            Stocks = stocks ?? new List<TradeStockState>();
        }

        public static SeedApplyResult FromCurrent(IReadOnlyList<TradeStockState> stocks)
        {
            var copied = new List<TradeStockState>();
            if (stocks != null)
            {
                for (var i = 0; i < stocks.Count; i++)
                {
                    if (stocks[i] != null)
                    {
                        copied.Add(stocks[i]);
                    }
                }
            }

            return new SeedApplyResult(copied);
        }
    }
}
