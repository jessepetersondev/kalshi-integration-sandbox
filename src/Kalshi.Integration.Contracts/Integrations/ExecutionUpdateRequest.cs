namespace Kalshi.Integration.Contracts.Integrations;

public sealed record ExecutionUpdateRequest(
    Guid OrderId,
    string Status,
    int FilledQuantity,
    DateTimeOffset? OccurredAt,
    string? CorrelationId);
