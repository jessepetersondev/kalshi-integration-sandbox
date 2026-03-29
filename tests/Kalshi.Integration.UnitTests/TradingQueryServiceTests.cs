using Kalshi.Integration.Application.Abstractions;
using Kalshi.Integration.Application.Trading;
using Kalshi.Integration.Domain.Executions;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.Positions;
using Kalshi.Integration.Domain.TradeIntents;
using Moq;

namespace Kalshi.Integration.UnitTests;

public sealed class TradingQueryServiceTests
{
    [Fact]
    public async Task GetOrderAsync_ShouldReturnNullWhenRepositoryMisses()
    {
        var orderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        var positionSnapshotRepository = new Mock<IPositionSnapshotRepository>(MockBehavior.Strict);
        orderRepository
            .Setup(x => x.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = new TradingQueryService(orderRepository.Object, positionSnapshotRepository.Object);

        var result = await service.GetOrderAsync(Guid.NewGuid());

        Assert.Null(result);
        orderRepository.VerifyAll();
    }

    [Fact]
    public async Task GetOrderAsync_ShouldMapOrderAndSortEvents()
    {
        var tradeIntent = new TradeIntent("KXBTC", TradeSide.Yes, 2, 0.45m, "Breakout");
        var order = new Order(tradeIntent);
        var olderEvent = new ExecutionEvent(order.Id, OrderStatus.Accepted, 0, DateTimeOffset.UtcNow.AddMinutes(-2));
        var newerEvent = new ExecutionEvent(order.Id, OrderStatus.PartiallyFilled, 1, DateTimeOffset.UtcNow.AddMinutes(-1));
        var orderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        var positionSnapshotRepository = new Mock<IPositionSnapshotRepository>(MockBehavior.Strict);

        orderRepository
            .Setup(x => x.GetOrderAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orderRepository
            .Setup(x => x.GetOrderEventsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { newerEvent, olderEvent });

        var service = new TradingQueryService(orderRepository.Object, positionSnapshotRepository.Object);

        var result = await service.GetOrderAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result!.Id);
        Assert.Equal("pending", result.Status);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("accepted", result.Events[0].Status);
        Assert.Equal("partiallyfilled", result.Events[1].Status);
        orderRepository.VerifyAll();
    }

    [Fact]
    public async Task GetPositionsAsync_ShouldMapRepositoryPositions()
    {
        var orderRepository = new Mock<IOrderRepository>(MockBehavior.Strict);
        var positionSnapshotRepository = new Mock<IPositionSnapshotRepository>(MockBehavior.Strict);
        positionSnapshotRepository
            .Setup(x => x.GetPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PositionSnapshot("KXETH", TradeSide.No, 1, 0.22m, DateTimeOffset.UtcNow),
                new PositionSnapshot("KXBTC", TradeSide.Yes, 2, 0.45m, DateTimeOffset.UtcNow)
            });

        var service = new TradingQueryService(orderRepository.Object, positionSnapshotRepository.Object);

        var result = await service.GetPositionsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("KXBTC", result[0].Ticker);
        Assert.Equal("yes", result[0].Side);
        Assert.Equal("KXETH", result[1].Ticker);
        Assert.Equal("no", result[1].Side);
        positionSnapshotRepository.VerifyAll();
    }
}
