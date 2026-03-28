using Kalshi.Integration.Application.Abstractions;
using Kalshi.Integration.Application.Risk;
using Kalshi.Integration.Contracts.TradeIntents;
using Kalshi.Integration.Domain.TradeIntents;
using Microsoft.Extensions.Options;
using Moq;

namespace Kalshi.Integration.UnitTests;

public sealed class RiskEvaluatorTests
{
    [Fact]
    public async Task EvaluateTradeIntent_ShouldAcceptValidTradeIntent()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetTradeIntentByCorrelationIdAsync("corr-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradeIntent?)null);

        var evaluator = CreateEvaluator(repository.Object, new RiskOptions { MaxOrderSize = 5 });

        var result = await evaluator.EvaluateTradeIntentAsync(new CreateTradeIntentRequest("KXBTC", "yes", 2, 0.45m, "Breakout", "corr-1"));

        Assert.True(result.Accepted);
        Assert.Equal("accepted", result.Decision);
        Assert.Empty(result.Reasons);
        Assert.False(result.DuplicateCorrelationIdDetected);
        repository.VerifyAll();
    }

    [Fact]
    public async Task EvaluateTradeIntent_ShouldRejectInvalidInputCollection()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        var evaluator = CreateEvaluator(repository.Object, new RiskOptions { MaxOrderSize = 3, RejectDuplicateCorrelationIds = false });

        var result = await evaluator.EvaluateTradeIntentAsync(new CreateTradeIntentRequest(" ", "maybe", 0, 1.5m, " ", null));

        Assert.False(result.Accepted);
        Assert.Equal("rejected", result.Decision);
        Assert.Contains("Ticker is required.", result.Reasons);
        Assert.Contains("Side must be either 'yes' or 'no'.", result.Reasons);
        Assert.Contains("Quantity must be greater than zero.", result.Reasons);
        Assert.Contains("Limit price must be greater than 0 and less than or equal to 1.", result.Reasons);
        Assert.Contains("Strategy name is required.", result.Reasons);
        repository.Verify(
            x => x.GetTradeIntentByCorrelationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateTradeIntent_ShouldRejectOversizedOrders()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        var evaluator = CreateEvaluator(repository.Object, new RiskOptions { MaxOrderSize = 3 });

        var result = await evaluator.EvaluateTradeIntentAsync(new CreateTradeIntentRequest("KXBTC", "yes", 4, 0.45m, "Breakout", null));

        Assert.False(result.Accepted);
        Assert.Contains(result.Reasons, reason => reason.Contains("max order size", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateTradeIntent_ShouldRejectDuplicateCorrelationIds()
    {
        var existingIntent = new TradeIntent("KXBTC", TradeSide.Yes, 1, 0.40m, "Test", "dup-1");
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetTradeIntentByCorrelationIdAsync("dup-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIntent);

        var evaluator = CreateEvaluator(repository.Object, new RiskOptions());

        var result = await evaluator.EvaluateTradeIntentAsync(new CreateTradeIntentRequest("KXBTC", "no", 1, 0.60m, "Fade", "dup-1"));

        Assert.False(result.Accepted);
        Assert.True(result.DuplicateCorrelationIdDetected);
        Assert.Contains(result.Reasons, reason => reason.Contains("already been used", StringComparison.OrdinalIgnoreCase));
        repository.VerifyAll();
    }

    [Fact]
    public async Task EvaluateTradeIntent_ShouldSkipDuplicateLookupWhenFeatureDisabled()
    {
        var repository = new Mock<ITradingRepository>(MockBehavior.Strict);
        var evaluator = CreateEvaluator(repository.Object, new RiskOptions { RejectDuplicateCorrelationIds = false });

        var result = await evaluator.EvaluateTradeIntentAsync(new CreateTradeIntentRequest("KXBTC", "yes", 1, 0.32m, "Scalp", "corr-disabled"));

        Assert.True(result.Accepted);
        repository.Verify(
            x => x.GetTradeIntentByCorrelationIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static RiskEvaluator CreateEvaluator(ITradingRepository repository, RiskOptions options)
        => new(repository, Options.Create(options));
}
