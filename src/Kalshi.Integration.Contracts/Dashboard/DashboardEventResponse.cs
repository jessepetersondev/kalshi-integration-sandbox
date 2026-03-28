namespace Kalshi.Integration.Contracts.Dashboard;

public sealed record DashboardEventResponse(
    Guid OrderId,
    string Ticker,
    string Status,
    int FilledQuantity,
    DateTimeOffset OccurredAt);
