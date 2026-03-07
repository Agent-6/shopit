namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Represents the interface for aggregate roots.
/// An aggregate root is the root entity of an aggregate that controls access to the aggregate's internal entities.
/// It is responsible for maintaining the invariants within the aggregate and publishing domain events.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public interface IAggregateRoot<out TId> : IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the collection of domain events that have occurred within this aggregate.
    /// These events should be published after the aggregate is committed to the database.
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from this aggregate.
    /// Should be called after all events have been published.
    /// </summary>
    void ClearDomainEvents();
}
