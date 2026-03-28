namespace Kalshi.Integration.Contracts.Orders;

public sealed record OrderResponse(
    Guid Id,
    Guid TradeIntentId,
    string Ticker,
    string Side,
    int Quantity,
    decimal LimitPrice,
    string StrategyName,
    string Status,
    int FilledQuantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<OrderEventResponse> Events);

public sealed record OrderEventResponse(
    string Status,
    int FilledQuantity,
    DateTimeOffset OccurredAt);
