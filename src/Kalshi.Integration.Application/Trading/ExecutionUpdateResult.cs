using Kalshi.Integration.Contracts.Orders;

namespace Kalshi.Integration.Application.Trading;

public sealed record ExecutionUpdateResult(
    Guid OrderId,
    string Status,
    int FilledQuantity,
    DateTimeOffset OccurredAt,
    OrderResponse Order);
