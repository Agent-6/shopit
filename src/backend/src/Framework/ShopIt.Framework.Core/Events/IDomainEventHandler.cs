using ShopIt.Framework.Domain.Events;

namespace ShopIt.Framework.Core.Events;

public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
