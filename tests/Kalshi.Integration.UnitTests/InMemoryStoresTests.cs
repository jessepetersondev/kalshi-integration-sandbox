using Kalshi.Integration.Application.Events;
using Kalshi.Integration.Application.Operations;
using Kalshi.Integration.Domain.Executions;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.Positions;
using Kalshi.Integration.Domain.TradeIntents;
using Kalshi.Integration.Infrastructure.Operations;
using Kalshi.Integration.Infrastructure.Persistence;

namespace Kalshi.Integration.UnitTests;

public sealed class InMemoryStoresTests
{
    [Fact]
    public async Task InMemoryTradingRepository_ShouldRoundTripTradeIntentsOrdersEventsAndPositions()
    {
        var repository = new InMemoryTradingRepository();
        var tradeIntent = new TradeIntent("KXBTC", TradeSide.Yes, 2, 0.45m, "Breakout", "corr-1");
        var order = new Order(tradeIntent);
        var position = new PositionSnapshot(tradeIntent.Ticker, tradeIntent.Side, 0, tradeIntent.LimitPrice, DateTimeOffset.UtcNow);
        var executionEvent = new ExecutionEvent(order.Id, order.CurrentStatus, order.FilledQuantity, DateTimeOffset.UtcNow);

        await repository.AddTradeIntentAsync(tradeIntent);
        await repository.AddOrderAsync(order);
        await repository.AddOrderEventAsync(executionEvent);
        await repository.UpsertPositionSnapshotAsync(position);

        var fetchedTradeIntent = await repository.GetTradeIntentAsync(tradeIntent.Id);
        var fetchedByCorrelationId = await repository.GetTradeIntentByCorrelationIdAsync("corr-1");
        var fetchedOrder = await repository.GetOrderAsync(order.Id);
        var events = await repository.GetOrderEventsAsync(order.Id);
        var positions = await repository.GetPositionsAsync();

        Assert.Same(tradeIntent, fetchedTradeIntent);
        Assert.Same(tradeIntent, fetchedByCorrelationId);
        Assert.Same(order, fetchedOrder);
        Assert.Single(events);
        Assert.Single(positions);
    }

    [Fact]
    public async Task InMemoryTradingRepository_ShouldReturnOrdersInUpdatedOrderDescending()
    {
        var repository = new InMemoryTradingRepository();
        var olderOrder = new Order(new TradeIntent("KXBTC", TradeSide.Yes, 1, 0.40m, "Breakout"));
        olderOrder.SetPersistenceState(olderOrder.Id, OrderStatus.Accepted, 0, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddMinutes(-10));
        var newerOrder = new Order(new TradeIntent("KXETH", TradeSide.No, 1, 0.55m, "Fade"));
        newerOrder.SetPersistenceState(newerOrder.Id, OrderStatus.Filled, 1, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddMinutes(-1));

        await repository.AddOrderAsync(olderOrder);
        await repository.AddOrderAsync(newerOrder);
        newerOrder.TransitionTo(OrderStatus.Settled);
        await repository.UpdateOrderAsync(newerOrder);

        var orders = await repository.GetOrdersAsync();

        Assert.Equal(newerOrder.Id, orders[0].Id);
        Assert.Equal(olderOrder.Id, orders[1].Id);
    }

    [Fact]
    public async Task InMemoryAuditRecordStore_ShouldFilterSortAndCapRequests()
    {
        var filteredStore = new InMemoryAuditRecordStore();
        var older = AuditRecord.Create("trading", "older", "accepted", "corr-1", "Older", occurredAt: DateTimeOffset.UtcNow.AddHours(-2));
        var newer = AuditRecord.Create("trading", "newer", "accepted", "corr-2", "Newer", occurredAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        var ignoredCategory = AuditRecord.Create("risk", "ignored", "rejected", "corr-3", "Ignored", occurredAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        await filteredStore.AddAsync(older);
        await filteredStore.AddAsync(newer);
        await filteredStore.AddAsync(ignoredCategory);

        var filtered = await filteredStore.GetRecentAsync("trading", hours: 24, limit: 25);

        Assert.Equal(2, filtered.Count);
        Assert.Equal(newer.Id, filtered[0].Id);
        Assert.Equal(older.Id, filtered[1].Id);

        var cappedStore = new InMemoryAuditRecordStore();
        for (var index = 0; index < 1002; index++)
        {
            await cappedStore.AddAsync(AuditRecord.Create("bulk", $"action-{index}", "accepted", $"corr-{index}", "bulk", occurredAt: DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        var capped = await cappedStore.GetRecentAsync(hours: 24, limit: 999);

        Assert.Equal(500, capped.Count);
    }

    [Fact]
    public async Task InMemoryOperationalIssueStore_ShouldFilterAndTrimToFiveHundred()
    {
        var filteredStore = new InMemoryOperationalIssueStore();
        var older = OperationalIssue.Create("validation", "warning", "risk", "older", occurredAt: DateTimeOffset.UtcNow.AddHours(-30));
        var recent = OperationalIssue.Create("validation", "error", "risk", "recent", occurredAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var otherCategory = OperationalIssue.Create("integration", "error", "gateway", "other", occurredAt: DateTimeOffset.UtcNow.AddMinutes(-4));

        await filteredStore.AddAsync(older);
        await filteredStore.AddAsync(recent);
        await filteredStore.AddAsync(otherCategory);

        var filtered = await filteredStore.GetRecentAsync("validation", hours: 24);

        var issue = Assert.Single(filtered);
        Assert.Equal(recent.Id, issue.Id);

        var trimmedStore = new InMemoryOperationalIssueStore();
        for (var index = 0; index < 501; index++)
        {
            await trimmedStore.AddAsync(OperationalIssue.Create("bulk", "warning", "worker", $"bulk-{index}", occurredAt: DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        var trimmed = await trimmedStore.GetRecentAsync("bulk", hours: 24);

        Assert.Equal(500, trimmed.Count);
    }

    [Fact]
    public async Task InMemoryApplicationEventPublisher_ShouldStopDispatchAfterDisposeAndResetState()
    {
        var publisher = new InMemoryApplicationEventPublisher();
        var received = new List<ApplicationEventEnvelope>();

        using (publisher.Subscribe((applicationEvent, cancellationToken) =>
               {
                   received.Add(applicationEvent);
                   return Task.CompletedTask;
               }))
        {
            await publisher.PublishAsync(ApplicationEventEnvelope.Create("trading", "order.created"));
        }

        await publisher.PublishAsync(ApplicationEventEnvelope.Create("trading", "order.updated"));

        Assert.Single(received);
        Assert.Equal(2, publisher.GetPublishedEvents().Count);

        publisher.Reset();

        Assert.Empty(publisher.GetPublishedEvents());
    }
}
