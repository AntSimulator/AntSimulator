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

            
            for (var i = 0; i < count; i++)
            {
                var candle = stock.candles[i];
                labels[i] = candle.tick.ToString();
                closes[i] = candle.close;
                volumes[i] = candle.volume;
                ohlc[i] = new OhlcPoint(candle.open, candle.close, candle.low, candle.high);
            }

            return new StockHistoryProjectionResult(stock.name, labels, closes, volumes, ohlc);
        }
    }
}
