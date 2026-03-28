namespace Kalshi.Integration.Infrastructure.Persistence.Entities;

public sealed class OrderEventEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int FilledQuantity { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
