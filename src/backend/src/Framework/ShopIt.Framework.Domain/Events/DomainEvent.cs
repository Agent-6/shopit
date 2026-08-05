using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Domain.Events;

/// <summary>
/// Represents a domain event that occurs within the system.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this domain event.
    /// </summary>
    public Guid EventId { get; } = DomainProviders.Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; } = DomainProviders.Date.UtcNow;
}
