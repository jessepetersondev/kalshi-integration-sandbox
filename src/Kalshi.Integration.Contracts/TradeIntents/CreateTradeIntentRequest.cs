namespace Kalshi.Integration.Contracts.TradeIntents;

public sealed record CreateTradeIntentRequest(
    string Ticker,
    string Side,
    int Quantity,
    decimal LimitPrice,
    string StrategyName,
    string? CorrelationId);
