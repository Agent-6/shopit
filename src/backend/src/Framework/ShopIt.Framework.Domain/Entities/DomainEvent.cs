namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Base class for domain events.
/// Provides common properties and functionality for all domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this domain event.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Initializes a new instance of the DomainEvent class.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the DomainEvent class with a specific event ID.
    /// </summary>
    /// <param name="eventId">The unique identifier for this event.</param>
    /// <param name="occurredOn">The timestamp when the event occurred.</param>
    protected DomainEvent(Guid eventId, DateTime occurredOn)
    {
        EventId = eventId;
        OccurredOn = occurredOn;
    }
}
