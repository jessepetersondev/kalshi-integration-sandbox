namespace Kalshi.Integration.Contracts.Dashboard;

public sealed record DashboardOrderSummaryResponse(
    Guid Id,
    string Ticker,
    string Side,
    int Quantity,
    decimal LimitPrice,
    string StrategyName,
    string Status,
    int FilledQuantity,
    DateTimeOffset UpdatedAt);
