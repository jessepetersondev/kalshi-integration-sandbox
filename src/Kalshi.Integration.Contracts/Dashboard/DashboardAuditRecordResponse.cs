namespace Kalshi.Integration.Contracts.Dashboard;

public sealed record DashboardAuditRecordResponse(
    Guid Id,
    string Category,
    string Action,
    string Outcome,
    string CorrelationId,
    string? IdempotencyKey,
    string? ResourceId,
    string Details,
    DateTimeOffset OccurredAt);
