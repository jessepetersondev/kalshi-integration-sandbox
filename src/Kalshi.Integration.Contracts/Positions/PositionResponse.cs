namespace Kalshi.Integration.Contracts.Positions;

public sealed record PositionResponse(
    string Ticker,
    string Side,
    int Contracts,
    decimal AveragePrice,
    DateTimeOffset AsOf);
