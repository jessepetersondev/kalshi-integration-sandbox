namespace Kalshi.Integration.Infrastructure.Persistence.Entities;

public sealed class TradeIntentEntity
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal LimitPrice { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
