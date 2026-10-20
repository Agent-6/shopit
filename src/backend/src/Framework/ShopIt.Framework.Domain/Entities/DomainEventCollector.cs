using ShopIt.Framework.Domain.Events;

namespace ShopIt.Framework.Domain.Entities;

/// <summary>
/// Holds an entity's pending domain events for entities that cannot inherit
/// <see cref="AggregateRoot{TId}"/>.
/// </summary>
/// <remarks>
/// C# has single inheritance, and some aggregates must inherit a framework type they do not own —
/// Identity's <c>User</c> and <c>Role</c> inherit <c>IdentityUser&lt;Guid&gt;</c> and
/// <c>IdentityRole&lt;Guid&gt;</c> (see ADR-0004). Those entities compose this collector instead of
/// keeping a private list and re-implementing raise/clear on each type, so the behaviour is written
/// once and the entity only exposes it.
/// </remarks>
public sealed class DomainEventCollector
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>
    /// The events raised so far, in order.
    /// </summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// Records an event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The event.</param>
    public void Raise(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears the collected events, after they have been dispatched.
    /// </summary>
    public void Clear() => _domainEvents.Clear();
}
