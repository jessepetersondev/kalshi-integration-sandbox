using Kalshi.Integration.Domain.Common;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.TradeIntents;

namespace Kalshi.Integration.UnitTests;

public sealed class OrderTests
{
    [Fact]
    public void Order_ShouldStartInPendingStatus()
    {
        var intent = new TradeIntent("KXBTC", TradeSide.Yes, 2, 0.45m, "Breakout");
        var order = new Order(intent);

        Assert.Equal(OrderStatus.Pending, order.CurrentStatus);
        Assert.Equal(0, order.FilledQuantity);
    }

    [Fact]
    public void TransitionTo_ShouldAllowPendingToAcceptedToRestingToPartiallyFilledToFilledToSettled()
    {
        var intent = new TradeIntent("KXBTC", TradeSide.Yes, 2, 0.45m, "Breakout");
        var order = new Order(intent);

        order.TransitionTo(OrderStatus.Accepted);
        order.TransitionTo(OrderStatus.Resting);
        order.TransitionTo(OrderStatus.PartiallyFilled, filledQuantity: 1);
        order.TransitionTo(OrderStatus.Filled, filledQuantity: 2);
        order.TransitionTo(OrderStatus.Settled);

        Assert.Equal(OrderStatus.Settled, order.CurrentStatus);
        Assert.Equal(2, order.FilledQuantity);
    }

    [Fact]
    public void TransitionTo_ShouldRejectIllegalStatusChanges()
    {
        var intent = new TradeIntent("KXBTC", TradeSide.No, 1, 0.55m, "Fade");
        var order = new Order(intent);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.Filled, filledQuantity: 1));
    }

    [Fact]
    public void TransitionTo_ShouldRejectFilledStatusWhenQuantityIsNotComplete()
    {
        var intent = new TradeIntent("KXBTC", TradeSide.Yes, 3, 0.40m, "Trend");
        var order = new Order(intent);

        order.TransitionTo(OrderStatus.Accepted);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.Filled, filledQuantity: 2));
    }
}
