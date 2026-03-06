namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Represents a marker interface for domain events.
/// Domain events capture something that has happened in the domain that is of interest to other parts of the system.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this domain event.
    /// </summary>
    Guid EventId => Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOn => DateTime.UtcNow;
}
