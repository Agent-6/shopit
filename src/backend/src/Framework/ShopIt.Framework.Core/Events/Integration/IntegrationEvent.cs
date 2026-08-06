using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Core.Events.Integration;

/// <summary>
/// Base record for all integration events that are published across service boundaries via Kafka.
/// Integration events are written to the transactional outbox and consumed via the inbox pattern.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Gets the unique identifier for this event instance.</summary>
    public Guid EventId { get; } = DomainProviders.Guid.NewGuid();

    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    public DateTime OccurredOn { get; } = DomainProviders.Date.UtcNow;

    /// <summary>
    /// Gets the event type name, derived automatically from the concrete record's type name.
    /// Used as the Kafka topic or message type discriminator.
    /// </summary>
    public string EventType => GetType().Name;
}
