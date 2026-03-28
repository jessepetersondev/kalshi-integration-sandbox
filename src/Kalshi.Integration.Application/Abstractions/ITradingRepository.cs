using Kalshi.Integration.Domain.Executions;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.Positions;
using Kalshi.Integration.Domain.TradeIntents;

namespace Kalshi.Integration.Application.Abstractions;

public interface ITradingRepository
{
    Task AddTradeIntentAsync(TradeIntent tradeIntent, CancellationToken cancellationToken = default);
    Task<TradeIntent?> GetTradeIntentAsync(Guid tradeIntentId, CancellationToken cancellationToken = default);
    Task<TradeIntent?> GetTradeIntentByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task AddOrderEventAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExecutionEvent>> GetOrderEventsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task UpsertPositionSnapshotAsync(PositionSnapshot positionSnapshot, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PositionSnapshot>> GetPositionsAsync(CancellationToken cancellationToken = default);
}
