using Kalshi.Integration.Application.Abstractions;
using Kalshi.Integration.Contracts.Orders;
using Kalshi.Integration.Domain.Orders;

namespace Kalshi.Integration.Application.Trading;

internal static class OrderResponseFactory
{
    public static async Task<OrderResponse> CreateAsync(Order order, IOrderRepository orderRepository, CancellationToken cancellationToken)
    {
        var events = await orderRepository.GetOrderEventsAsync(order.Id, cancellationToken);
        return new OrderResponse(
            order.Id,
            order.TradeIntent.Id,
            order.TradeIntent.Ticker,
            order.TradeIntent.Side.ToString().ToLowerInvariant(),
            order.TradeIntent.Quantity,
            order.TradeIntent.LimitPrice,
            order.TradeIntent.StrategyName,
            order.CurrentStatus.ToString().ToLowerInvariant(),
            order.FilledQuantity,
            order.CreatedAt,
            order.UpdatedAt,
            events
                .OrderBy(e => e.OccurredAt)
                .Select(e => new OrderEventResponse(e.Status.ToString().ToLowerInvariant(), e.FilledQuantity, e.OccurredAt))
                .ToArray());
    }
}
