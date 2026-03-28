using Kalshi.Integration.Application.Abstractions;
using Kalshi.Integration.Application.Risk;
using Kalshi.Integration.Application.Trading;
using Kalshi.Integration.Contracts.Integrations;
using Kalshi.Integration.Contracts.Orders;
using Kalshi.Integration.Contracts.TradeIntents;
using Kalshi.Integration.Domain.Common;
using Kalshi.Integration.Domain.Executions;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.Positions;
using Kalshi.Integration.Domain.TradeIntents;
using Microsoft.Extensions.Options;
using Moq;

namespace Kalshi.Integration.UnitTests;

public sealed class TradingServiceTests
{
    [Fact]
    public async Task CreateTradeIntentAsync_ShouldPersistTradeIntentAndReturnRiskDecisionResponse()
    {
        TradeIntent? capturedTradeIntent = null;
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetTradeIntentByCorrelationIdAsync("corr-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeIntent?)null);
        repository
            .Setup(x => x.AddTradeIntentAsync(It.IsAny<TradeIntent>(), It.IsAny<CancellationToken>()))
            .Callback<TradeIntent, CancellationToken>((tradeIntent, _) => capturedTradeIntent = tradeIntent)
            .Returns(Task.CompletedTask);

        var service = CreateService(repository.Object, maxOrderSize: 5);

        var response = await service.CreateTradeIntentAsync(new CreateTradeIntentRequest(" kxbtc ", "yes", 2, 0.45678m, " Breakout ", "corr-1"));

        Assert.NotNull(capturedTradeIntent);
        Assert.Equal(capturedTradeIntent!.Id, response.Id);
        Assert.Equal("KXBTC", response.Ticker);
        Assert.Equal("yes", response.Side);
        Assert.Equal(2, response.Quantity);
        Assert.Equal(0.4568m, response.LimitPrice);
        Assert.Equal("Breakout", response.StrategyName);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.True(response.RiskDecision.Accepted);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreateTradeIntentAsync_ShouldThrowWhenRiskEvaluatorRejectsRequest()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        var service = CreateService(repository.Object, maxOrderSize: 1);

        var action = () => service.CreateTradeIntentAsync(new CreateTradeIntentRequest("KXBTC", "yes", 2, 0.40m, "Breakout", null));

        var exception = await Assert.ThrowsAsync<DomainException>(action);
        Assert.Contains("max order size", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldPersistOrderArtifactsAndReturnMappedResponse()
    {
        var tradeIntent = new TradeIntent("KXBTC", TradeSide.Yes, 2, 0.45m, "Breakout", "corr-order");
        Order? capturedOrder = null;
        PositionSnapshot? capturedPosition = null;
        var events = new List<ExecutionEvent>();
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);

        repository
            .Setup(x => x.GetTradeIntentAsync(tradeIntent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tradeIntent);
        repository
            .Setup(x => x.AddOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.AddOrderEventAsync(It.IsAny<ExecutionEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ExecutionEvent, CancellationToken>((executionEvent, _) => events.Add(executionEvent))
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.UpsertPositionSnapshotAsync(It.IsAny<PositionSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<PositionSnapshot, CancellationToken>((snapshot, _) => capturedPosition = snapshot)
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.GetOrderEventsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid orderId, CancellationToken _) => (IReadOnlyList<ExecutionEvent>)events.Where(x => x.OrderId == orderId).ToArray());

        var service = CreateService(repository.Object);

        var response = await service.CreateOrderAsync(new CreateOrderRequest(tradeIntent.Id));

        Assert.NotNull(capturedOrder);
        Assert.Equal(capturedOrder!.Id, response.Id);
        Assert.Equal(tradeIntent.Id, response.TradeIntentId);
        Assert.Equal("pending", response.Status);
        Assert.Equal(0, response.FilledQuantity);
        Assert.Single(response.Events);
        Assert.Equal("pending", response.Events[0].Status);
        Assert.NotNull(capturedPosition);
        Assert.Equal("KXBTC", capturedPosition!.Ticker);
        Assert.Equal(TradeSide.Yes, capturedPosition.Side);
        Assert.Equal(0, capturedPosition.Contracts);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowWhenTradeIntentDoesNotExist()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetTradeIntentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeIntent?)null);

        var service = CreateService(repository.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateOrderAsync(new CreateOrderRequest(Guid.NewGuid())));
        repository.VerifyAll();
    }

    [Fact]
    public async Task ApplyExecutionUpdateAsync_ShouldUpdateOrderAndPositionAndReturnMappedResponse()
    {
        var tradeIntent = new TradeIntent("KXBTC", TradeSide.No, 3, 0.61m, "Fade", "corr-exec");
        var order = new Order(tradeIntent);
        order.TransitionTo(OrderStatus.Accepted, updatedAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        var firstEventTime = DateTimeOffset.UtcNow.AddMinutes(-4);
        var updateTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        var events = new List<ExecutionEvent>
        {
            new(order.Id, OrderStatus.Accepted, 0, firstEventTime)
        };
        PositionSnapshot? capturedPosition = null;
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);

        repository
            .Setup(x => x.GetOrderAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        repository
            .Setup(x => x.UpdateOrderAsync(order, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.AddOrderEventAsync(It.IsAny<ExecutionEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ExecutionEvent, CancellationToken>((executionEvent, _) => events.Add(executionEvent))
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.UpsertPositionSnapshotAsync(It.IsAny<PositionSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<PositionSnapshot, CancellationToken>((snapshot, _) => capturedPosition = snapshot)
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.GetOrderEventsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExecutionEvent>)events);

        var service = CreateService(repository.Object);

        var result = await service.ApplyExecutionUpdateAsync(new ExecutionUpdateRequest(order.Id, "partially-filled", 2, updateTime, "corr-exec"));

        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal("partiallyfilled", result.Status);
        Assert.Equal(2, result.FilledQuantity);
        Assert.Equal(updateTime, result.OccurredAt);
        Assert.Equal("partiallyfilled", result.Order.Status);
        Assert.Equal(2, result.Order.FilledQuantity);
        Assert.Equal(2, result.Order.Events.Count);
        Assert.True(result.Order.Events[0].OccurredAt <= result.Order.Events[1].OccurredAt);
        Assert.NotNull(capturedPosition);
        Assert.Equal("KXBTC", capturedPosition!.Ticker);
        Assert.Equal(TradeSide.No, capturedPosition.Side);
        Assert.Equal(2, capturedPosition.Contracts);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ApplyExecutionUpdateAsync_ShouldThrowWhenOrderDoesNotExist()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = CreateService(repository.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ApplyExecutionUpdateAsync(new ExecutionUpdateRequest(Guid.NewGuid(), "filled", 1, DateTimeOffset.UtcNow, null)));
        repository.VerifyAll();
    }

    [Fact]
    public async Task ApplyExecutionUpdateAsync_ShouldThrowForInvalidStatus()
    {
        var order = new Order(new TradeIntent("KXBTC", TradeSide.Yes, 1, 0.44m, "Breakout"));
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetOrderAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService(repository.Object);

        await Assert.ThrowsAsync<DomainException>(() => service.ApplyExecutionUpdateAsync(new ExecutionUpdateRequest(order.Id, "bad-status", 1, DateTimeOffset.UtcNow, null)));
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnNullWhenRepositoryMisses()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = CreateService(repository.Object);

        var result = await service.GetOrderAsync(Guid.NewGuid());

        Assert.Null(result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetPositionsAsync_ShouldMapRepositoryPositions()
    {
        var positions = new List<PositionSnapshot>
        {
            new("KXBTC", TradeSide.Yes, 2, 0.45m, DateTimeOffset.UtcNow),
            new("KXETH", TradeSide.No, 1, 0.22m, DateTimeOffset.UtcNow)
        };
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        var service = CreateService(repository.Object);

        var result = await service.GetPositionsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Ticker == "KXBTC" && x.Side == "yes");
        Assert.Contains(result, x => x.Ticker == "KXETH" && x.Side == "no");
        repository.VerifyAll();
    }

    private static TradingService CreateService(ITradingRepository repository, int maxOrderSize = 10)
    {
        var riskEvaluator = new RiskEvaluator(repository, Options.Create(new RiskOptions { MaxOrderSize = maxOrderSize }));
        return new TradingService(repository, riskEvaluator);
    }
}
