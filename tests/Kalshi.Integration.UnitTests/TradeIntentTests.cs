using Kalshi.Integration.Domain.Common;
using Kalshi.Integration.Domain.TradeIntents;

namespace Kalshi.Integration.UnitTests;

public sealed class TradeIntentTests
{
    [Fact]
    public void Constructor_ShouldNormalizeTickerAndPreserveBusinessData()
    {
        var intent = new TradeIntent(" kxbtc-yes ", TradeSide.Yes, 3, 0.54321m, "Mean Reversion");

        Assert.Equal("KXBTC-YES", intent.Ticker);
        Assert.Equal(TradeSide.Yes, intent.Side);
        Assert.Equal(3, intent.Quantity);
        Assert.Equal(0.5432m, intent.LimitPrice);
        Assert.Equal("Mean Reversion", intent.StrategyName);
        Assert.False(string.IsNullOrWhiteSpace(intent.CorrelationId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldRejectNonPositiveQuantity(int quantity)
    {
        Assert.Throws<DomainException>(() => new TradeIntent("KXBTC", TradeSide.No, quantity, 0.50m, "Test"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_ShouldRejectInvalidLimitPrice(decimal price)
    {
        Assert.Throws<DomainException>(() => new TradeIntent("KXBTC", TradeSide.No, 1, price, "Test"));
    }
}
