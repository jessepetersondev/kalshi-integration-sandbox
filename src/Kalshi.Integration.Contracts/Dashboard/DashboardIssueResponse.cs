namespace Kalshi.Integration.Contracts.Dashboard;

public sealed record DashboardIssueResponse(
    Guid Id,
    string Category,
    string Severity,
    string Source,
    string Message,
    string? Details,
    DateTimeOffset OccurredAt);
