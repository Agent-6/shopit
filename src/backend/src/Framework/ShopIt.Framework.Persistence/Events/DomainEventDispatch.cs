using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Framework.Persistence.Events;

/// <summary>
/// The one way pending domain events are dispatched, shared by the command path
/// (<see cref="UnitOfWork.UnitOfWork{TContext}"/>) and the inbox path
/// (<see cref="Inbox.InboxProcessor{TContext}"/>).
/// </summary>
/// <remarks>
/// <para>
/// Domain events live on tracked aggregates, not in a queue: collecting them means asking EF Core's
/// ChangeTracker what is loaded and changed. Both paths therefore need the same two steps — collect and
/// clear, then dispatch — and dispatching only from one of them is how the inbox path silently dropped
/// every event an inbox handler raised.
/// </para>
/// <para>
/// Clearing happens before dispatch so a handler that throws does not leave the event to be redispatched
/// by a later save on the same context; the transaction is what makes the failure atomic.
/// </para>
/// </remarks>
public static class DomainEventDispatch
{
    /// <summary>
    /// Collects the domain events raised by the aggregates tracked in <paramref name="dbContext"/>,
    /// clears them, and dispatches them.
    /// </summary>
    /// <param name="dbContext">The context whose tracked aggregates are inspected.</param>
    /// <param name="dispatcher">The dispatcher to invoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The events that were dispatched, in aggregate order.</returns>
    public static async Task<IReadOnlyList<DomainEvent>> DispatchPendingAsync(
        DbContext dbContext,
        IDomainEventDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var aggregates = dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
        {
            return [];
        }

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        await dispatcher.DispatchAsync(domainEvents, cancellationToken);

        return domainEvents;
    }
}
