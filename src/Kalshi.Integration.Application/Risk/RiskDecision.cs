namespace Kalshi.Integration.Application.Risk;

public sealed record RiskDecision(
    bool Accepted,
    string Decision,
    IReadOnlyList<string> Reasons,
    int MaxOrderSize,
    bool DuplicateCorrelationIdDetected);
