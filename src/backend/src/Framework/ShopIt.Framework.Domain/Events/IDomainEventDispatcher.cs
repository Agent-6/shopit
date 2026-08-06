namespace ShopIt.Framework.Domain.Events;

/// <summary>
/// Dispatches domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches the given domain events to all registered handlers, in order.
    /// All handlers execute within the caller's ambient transaction.
    /// </summary>
    /// <param name="events">The domain events to dispatch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DispatchAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);
}
