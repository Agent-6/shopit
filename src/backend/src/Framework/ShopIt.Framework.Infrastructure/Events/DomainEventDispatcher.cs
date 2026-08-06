using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Domain.Events;

namespace ShopIt.Framework.Infrastructure.Events;

/// <summary>
/// Dispatches domain events to all registered <see cref="IDomainEventHandler{TEvent}"/> implementations.
/// Handlers are resolved from the DI container, allowing them to use scoped services
/// (e.g. <see cref="ShopIt.Framework.Core.Events.Integration.IOutboxWriter"/>) within the same transaction.
/// </summary>
public class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger = logger;

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                    continue;

                _logger.LogDebug(
                    "Dispatching domain event {EventType} (EventId: {EventId}) to handler {HandlerType}",
                    eventType.Name, domainEvent.EventId, handler.GetType().Name);

                // Invoke HandleAsync via the non-generic interface method
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<DomainEvent>.HandleAsync))!;
                var task = (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
                await task;
            }
        }
    }
}
