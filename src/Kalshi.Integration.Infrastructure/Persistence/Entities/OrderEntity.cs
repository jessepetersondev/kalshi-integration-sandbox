namespace Kalshi.Integration.Infrastructure.Persistence.Entities;

public sealed class OrderEntity
{
    public Guid Id { get; set; }
    public Guid TradeIntentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int FilledQuantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
