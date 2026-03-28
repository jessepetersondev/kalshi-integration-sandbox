using Kalshi.Integration.Application.Abstractions;
using Kalshi.Integration.Application.Risk;
using Kalshi.Integration.Contracts.TradeIntents;
using Kalshi.Integration.Domain.Executions;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.Positions;
using Kalshi.Integration.Domain.TradeIntents;
using Microsoft.Extensions.Options;

namespace Kalshi.Integration.UnitTests;

public sealed class RiskEvaluatorTests
{
    [Fact]
    public async Task EvaluateTradeIntent_ShouldRejectOversizedOrders()
    {
        var repository = new FakeTradingRepository();
        var evaluator = new RiskEvaluator(repository, Options.Create(new RiskOptions { MaxOrderSize = 3 }));

        var result = await evaluator.EvaluateTradeIntentAsync(new CreateTradeIntentRequest("KXBTC", "yes", 4, 0.45m, "Breakout", "corr-1"));

        Assert.False(result.Accepted);
        Assert.Contains(result.Reasons, reason => reason.Contains("max order size", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateTradeIntent_ShouldRejectDuplicateCorrelationIds()
    {
        var repository = new FakeTradingRepository();
        await repository.AddTradeIntentAsync(new TradeIntent("KXBTC", TradeSide.Yes, 1, 0.40m, "Test", "dup-1"));
        var evaluator = new RiskEvaluator(repository, Options.Create(new RiskOptions()));

        var result = await evaluator.EvaluateTradeIntentAsync(new CreateTradeIntentRequest("KXBTC", "no", 1, 0.60m, "Fade", "dup-1"));

        Assert.False(result.Accepted);
        Assert.True(result.DuplicateCorrelationIdDetected);
    }

    private sealed class FakeTradingRepository : ITradingRepository
    {
        private readonly List<TradeIntent> _tradeIntents = [];

        public Task AddTradeIntentAsync(TradeIntent tradeIntent, CancellationToken cancellationToken = default)
        {
            _tradeIntents.Add(tradeIntent);
            return Task.CompletedTask;
        }

        public Task<TradeIntent?> GetTradeIntentAsync(Guid tradeIntentId, CancellationToken cancellationToken = default)
            => Task.FromResult(_tradeIntents.SingleOrDefault(x => x.Id == tradeIntentId));

        public Task<TradeIntent?> GetTradeIntentByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
            => Task.FromResult(_tradeIntents.SingleOrDefault(x => x.CorrelationId == correlationId));

        public Task AddOrderAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateOrderAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<IReadOnlyList<Order>> GetOrdersAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Order>>([]);
        public Task AddOrderEventAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ExecutionEvent>> GetOrderEventsAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExecutionEvent>>([]);
        public Task UpsertPositionSnapshotAsync(PositionSnapshot positionSnapshot, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<PositionSnapshot>> GetPositionsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PositionSnapshot>>([]);
    }
}
