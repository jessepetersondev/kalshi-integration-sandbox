namespace Kalshi.Integration.Contracts.TradeIntents;

public sealed record RiskDecisionResponse(
    bool Accepted,
    string Decision,
    string[] Reasons,
    int MaxOrderSize,
    bool DuplicateCorrelationIdDetected);
