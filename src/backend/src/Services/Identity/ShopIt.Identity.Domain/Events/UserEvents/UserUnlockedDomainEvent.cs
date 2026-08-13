using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserUnlockedDomainEvent(Guid UserId) : DomainEvent;
