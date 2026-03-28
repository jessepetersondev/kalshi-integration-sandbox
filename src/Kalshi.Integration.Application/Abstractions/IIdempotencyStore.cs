using Kalshi.Integration.Application.Operations;

namespace Kalshi.Integration.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string scope, string key, CancellationToken cancellationToken = default);
    Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
