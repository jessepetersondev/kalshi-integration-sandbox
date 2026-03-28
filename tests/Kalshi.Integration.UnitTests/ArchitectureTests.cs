using Kalshi.Integration.Application.Operations;
using Kalshi.Integration.Infrastructure.Operations;

namespace Kalshi.Integration.UnitTests;

public sealed class IdempotencyServiceTests
{
    [Fact]
    public async Task LookupAsync_ShouldReplayWhenKeyAndPayloadMatchSavedResponse()
    {
        var store = new InMemoryIdempotencyStore();
        var service = new IdempotencyService(store);
        var request = new { ticker = "KXBTC", side = "yes", quantity = 1 };

        await service.SaveResponseAsync("trade-intents", "idem-1", request, 201, new { id = Guid.NewGuid(), ticker = "KXBTC" });
        var result = await service.LookupAsync("trade-intents", "idem-1", request);

        Assert.Equal(IdempotencyLookupStatus.Replay, result.Status);
        Assert.NotNull(result.Record);
        Assert.Equal(201, result.Record!.StatusCode);
    }

    [Fact]
    public async Task LookupAsync_ShouldConflictWhenKeyMatchesButPayloadDiffers()
    {
        var store = new InMemoryIdempotencyStore();
        var service = new IdempotencyService(store);

        await service.SaveResponseAsync("orders", "idem-2", new { tradeIntentId = Guid.NewGuid() }, 201, new { id = Guid.NewGuid() });
        var result = await service.LookupAsync("orders", "idem-2", new { tradeIntentId = Guid.NewGuid() });

        Assert.Equal(IdempotencyLookupStatus.Conflict, result.Status);
        Assert.NotNull(result.Record);
    }
}
