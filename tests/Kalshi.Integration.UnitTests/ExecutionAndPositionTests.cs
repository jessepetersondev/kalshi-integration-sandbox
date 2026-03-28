using Kalshi.Integration.Domain.Common;
using Kalshi.Integration.Domain.Executions;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.Positions;
using Kalshi.Integration.Domain.TradeIntents;

namespace Kalshi.Integration.UnitTests;

public sealed class ExecutionAndPositionTests
{
    [Fact]
    public void ExecutionEvent_ShouldCaptureOrderEventData()
    {
        var orderId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;

        var executionEvent = new ExecutionEvent(orderId, OrderStatus.PartiallyFilled, 1, occurredAt);

        Assert.Equal(orderId, executionEvent.OrderId);
        Assert.Equal(OrderStatus.PartiallyFilled, executionEvent.Status);
        Assert.Equal(1, executionEvent.FilledQuantity);
        Assert.Equal(occurredAt, executionEvent.OccurredAt);
    }

    [Fact]
    public void ExecutionEvent_ShouldRejectNegativeFilledQuantity()
    {
        Assert.Throws<DomainException>(() => new ExecutionEvent(Guid.NewGuid(), OrderStatus.Accepted, -1, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void PositionSnapshot_ShouldNormalizeTickerAndRoundAveragePrice()
    {
        var snapshot = new PositionSnapshot(" kxbtc-26mar2808 ", TradeSide.No, 4, 0.65439m, DateTimeOffset.UtcNow);

        Assert.Equal("KXBTC-26MAR2808", snapshot.Ticker);
        Assert.Equal(TradeSide.No, snapshot.Side);
        Assert.Equal(4, snapshot.Contracts);
        Assert.Equal(0.6544m, snapshot.AveragePrice);
    }
}
