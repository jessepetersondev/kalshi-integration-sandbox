using Kalshi.Integration.Application.Events;

namespace Kalshi.Integration.Application.Abstractions;

public interface IApplicationEventPublisher
{
    Task PublishAsync(ApplicationEventEnvelope applicationEvent, CancellationToken cancellationToken = default);
}
