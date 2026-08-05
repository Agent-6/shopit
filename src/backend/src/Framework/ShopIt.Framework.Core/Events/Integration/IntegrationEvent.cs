using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Core.Events.Integration;

public abstract record IntegrationEvent
{
    public Guid EventId { get; } = DomainProviders.Guid.NewGuid();
    public DateTime OccurredOn { get; } = DomainProviders.Date.UtcNow;
    public required string EventType { get; init; }
}
