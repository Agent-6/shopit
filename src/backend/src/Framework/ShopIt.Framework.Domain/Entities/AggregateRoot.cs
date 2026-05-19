using System.Text.Json.Serialization;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Base class for aggregate roots.
/// Provides functionality for managing domain events and enforcing aggregate boundaries.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the collection of domain events that have been raised within this aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Private constructor for EF Core and JSON serialization.
    /// </summary>
    [JsonConstructor]
    protected AggregateRoot() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the AggregateRoot class.
    /// </summary>
    /// <param name="id">The unique identifier for this aggregate root.</param>
    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Raises a domain event to be published.
    /// These events will be published after the aggregate is persisted to the database.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from this aggregate.
    /// Should be called after all events have been published.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
