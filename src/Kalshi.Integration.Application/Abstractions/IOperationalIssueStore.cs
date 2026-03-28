using Kalshi.Integration.Application.Operations;

namespace Kalshi.Integration.Application.Abstractions;

public interface IOperationalIssueStore
{
    Task AddAsync(OperationalIssue issue, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalIssue>> GetRecentAsync(string? category = null, int hours = 24, CancellationToken cancellationToken = default);
}
