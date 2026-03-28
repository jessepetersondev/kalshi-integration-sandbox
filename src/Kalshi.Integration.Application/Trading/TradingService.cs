using Kalshi.Integration.Application.Abstractions;
using Kalshi.Integration.Application.Risk;
using Kalshi.Integration.Contracts.Integrations;
using Kalshi.Integration.Contracts.Orders;
using Kalshi.Integration.Contracts.Positions;
using Kalshi.Integration.Contracts.TradeIntents;
using Kalshi.Integration.Domain.Common;
using Kalshi.Integration.Domain.Executions;
using Kalshi.Integration.Domain.Orders;
using Kalshi.Integration.Domain.Positions;
using Kalshi.Integration.Domain.TradeIntents;

namespace Kalshi.Integration.Application.Trading;

public sealed class TradingService
{
    private readonly ITradingRepository _repository;
    private readonly RiskEvaluator _riskEvaluator;

    public TradingService(ITradingRepository repository, RiskEvaluator riskEvaluator)
    {
        _repository = repository;
        _riskEvaluator = riskEvaluator;
    }

    public async Task<TradeIntentResponse> CreateTradeIntentAsync(CreateTradeIntentRequest request, CancellationToken cancellationToken = default)
    {
        var riskDecision = await _riskEvaluator.EvaluateTradeIntentAsync(request, cancellationToken);
        if (!riskDecision.Accepted)
        {
            throw new DomainException(string.Join(" ", riskDecision.Reasons));
        }

        var tradeIntent = new TradeIntent(
            request.Ticker,
            ParseSide(request.Side),
            request.Quantity,
            request.LimitPrice,
            request.StrategyName,
            request.CorrelationId);

        await _repository.AddTradeIntentAsync(tradeIntent, cancellationToken);

        return new TradeIntentResponse(
            tradeIntent.Id,
            tradeIntent.Ticker,
            tradeIntent.Side.ToString().ToLowerInvariant(),
            tradeIntent.Quantity,
            tradeIntent.LimitPrice,
            tradeIntent.StrategyName,
            tradeIntent.CorrelationId,
            tradeIntent.CreatedAt,
            new RiskDecisionResponse(
                riskDecision.Accepted,
                riskDecision.Decision,
                riskDecision.Reasons.ToArray(),
                riskDecision.MaxOrderSize,
                riskDecision.DuplicateCorrelationIdDetected));
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var tradeIntent = await _repository.GetTradeIntentAsync(request.TradeIntentId, cancellationToken);
        if (tradeIntent is null)
        {
            throw new KeyNotFoundException($"Trade intent '{request.TradeIntentId}' was not found.");
        }

        var order = new Order(tradeIntent);
        await _repository.AddOrderAsync(order, cancellationToken);
        await _repository.AddOrderEventAsync(new ExecutionEvent(order.Id, order.CurrentStatus, order.FilledQuantity, order.CreatedAt), cancellationToken);
        await _repository.UpsertPositionSnapshotAsync(new PositionSnapshot(tradeIntent.Ticker, tradeIntent.Side, 0, tradeIntent.LimitPrice, order.UpdatedAt), cancellationToken);

        return await BuildOrderResponseAsync(order, cancellationToken);
    }

    public async Task<ExecutionUpdateResult> ApplyExecutionUpdateAsync(ExecutionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetOrderAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order '{request.OrderId}' was not found.");
        }

        var status = ParseOrderStatus(request.Status);
        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;

        order.TransitionTo(status, request.FilledQuantity, occurredAt);
        await _repository.UpdateOrderAsync(order, cancellationToken);

        var executionEvent = new ExecutionEvent(order.Id, status, order.FilledQuantity, occurredAt);
        await _repository.AddOrderEventAsync(executionEvent, cancellationToken);

        await _repository.UpsertPositionSnapshotAsync(
            new PositionSnapshot(
                order.TradeIntent.Ticker,
                order.TradeIntent.Side,
                order.FilledQuantity,
                order.TradeIntent.LimitPrice,
                occurredAt),
            cancellationToken);

        var orderResponse = await BuildOrderResponseAsync(order, cancellationToken);
        return new ExecutionUpdateResult(order.Id, status.ToString().ToLowerInvariant(), order.FilledQuantity, occurredAt, orderResponse);
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        return await BuildOrderResponseAsync(order, cancellationToken);
    }

    public async Task<IReadOnlyList<PositionResponse>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        var positions = await _repository.GetPositionsAsync(cancellationToken);
        return positions
            .Select(position => new PositionResponse(
                position.Ticker,
                position.Side.ToString().ToLowerInvariant(),
                position.Contracts,
                position.AveragePrice,
                position.AsOf))
            .ToArray();
    }

    private async Task<OrderResponse> BuildOrderResponseAsync(Order order, CancellationToken cancellationToken)
    {
        var events = await _repository.GetOrderEventsAsync(order.Id, cancellationToken);
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

    private static TradeSide ParseSide(string side)
    {
        if (Enum.TryParse<TradeSide>(side, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new DomainException("Side must be either 'yes' or 'no'.");
    }

    private static OrderStatus ParseOrderStatus(string status)
    {
        if (Enum.TryParse<OrderStatus>(status.Replace("-", string.Empty).Replace("_", string.Empty), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new DomainException("Execution update status is invalid.");
    }
}
