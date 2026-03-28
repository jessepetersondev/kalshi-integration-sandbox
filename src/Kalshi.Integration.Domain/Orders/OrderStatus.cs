namespace Kalshi.Integration.Domain.Orders;

public enum OrderStatus
{
    Pending = 1,
    Accepted = 2,
    Resting = 3,
    PartiallyFilled = 4,
    Filled = 5,
    Canceled = 6,
    Rejected = 7,
    Settled = 8
}
