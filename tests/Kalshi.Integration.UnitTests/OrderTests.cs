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
    public void SetPersistenceState_ShouldOverridePersistedFields()
    {
        var createdAt = new DateTimeOffset(2026, 3, 28, 12, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(5);
        var order = new Order(new TradeIntent("KXBTC", TradeSide.Yes, 2, 0.45m, "Breakout"));
        var id = Guid.NewGuid();

        order.SetPersistenceState(id, OrderStatus.Resting, 1, createdAt, updatedAt);

        Assert.Equal(id, order.Id);
        Assert.Equal(OrderStatus.Resting, order.CurrentStatus);
        Assert.Equal(1, order.FilledQuantity);
        Assert.Equal(createdAt, order.CreatedAt);
        Assert.Equal(updatedAt, order.UpdatedAt);
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
    public void TransitionTo_ShouldRejectNegativeFilledQuantity()
    {
        var order = CreateAcceptedOrder(quantity: 3);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.PartiallyFilled, filledQuantity: -1));
    }

    [Fact]
    public void TransitionTo_ShouldRejectFilledQuantityMovingBackward()
    {
        var order = CreateAcceptedOrder(quantity: 3);
        order.TransitionTo(OrderStatus.PartiallyFilled, filledQuantity: 2);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.PartiallyFilled, filledQuantity: 1));
    }

    [Fact]
    public void TransitionTo_ShouldRejectFilledQuantityExceedingOrderQuantity()
    {
        var order = CreateAcceptedOrder(quantity: 3);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.PartiallyFilled, filledQuantity: 4));
    }

    [Fact]
    public void TransitionTo_ShouldRejectFilledStatusWhenQuantityIsNotComplete()
    {
        var order = CreateAcceptedOrder(quantity: 3);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.Filled, filledQuantity: 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void TransitionTo_ShouldRejectPartiallyFilledWithoutPartialQuantity(int filledQuantity)
    {
        var order = CreateAcceptedOrder(quantity: 3);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.PartiallyFilled, filledQuantity));
    }

    private static Order CreateAcceptedOrder(int quantity)
    {
        var order = new Order(new TradeIntent("KXBTC", TradeSide.Yes, quantity, 0.40m, "Trend"));
        order.TransitionTo(OrderStatus.Accepted);
        return order;
    }
}
