using System;
using Stocks.Models;

namespace Stocks.Core
{
    public readonly struct OhlcPoint
    {
        public double Open { get; }
        public double Close { get; }
        public double Low { get; }
        public double High { get; }

        public OhlcPoint(double open, double close, double low, double high)
        {
            Open = open;
            Close = close;
            Low = low;
            High = high;
        }
    }

    public sealed class StockHistoryProjectionResult
    {
        public string DisplayName { get; }
        public string[] XLabels { get; }
        public double[] Closes { get; }
        public double[] Volumes { get; }
        public OhlcPoint[] Ohlc { get; }

        public StockHistoryProjectionResult(
            string displayName,
            string[] xLabels,
            double[] closes,
            double[] volumes,
            OhlcPoint[] ohlc)
        {
            DisplayName = displayName ?? string.Empty;
            XLabels = xLabels ?? Array.Empty<string>();
            Closes = closes ?? Array.Empty<double>();
            Volumes = volumes ?? Array.Empty<double>();
            Ohlc = ohlc ?? Array.Empty<OhlcPoint>();
        }
    }

    public static class StockHistoryProjection
    {
        public static StockHistoryProjectionResult Build(StockHistory10D stock)
        {
            if (stock?.candles == null || stock.candles.Count == 0)
            {
                return new StockHistoryProjectionResult(
                    stock?.name ?? string.Empty,
                    Array.Empty<string>(),
                    Array.Empty<double>(),
                    Array.Empty<double>(),
                    Array.Empty<OhlcPoint>());
            }

            var count = stock.candles.Count;
            var labels = new string[count];
            var closes = new double[count];
            var volumes = new double[count];
            var ohlc = new OhlcPoint[count];

            var previousClose = (double)stock.candles[0].price;
            for (var i = 0; i < count; i++)
            {
                var candle = stock.candles[i];
                var close = candle.price;
                var open = i == 0 ? close : previousClose;
                var high = Math.Max(open, close) * 1.01d;
                var low = Math.Min(open, close) * 0.99d;

                labels[i] = candle.tick.ToString();
                closes[i] = close;
                volumes[i] = candle.volume;
                ohlc[i] = new OhlcPoint(open, close, low, high);

                previousClose = close;
            }

            return new StockHistoryProjectionResult(stock.name, labels, closes, volumes, ohlc);
        }
    }
}
