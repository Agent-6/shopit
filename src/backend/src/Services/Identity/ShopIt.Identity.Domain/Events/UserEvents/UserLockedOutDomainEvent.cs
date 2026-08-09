using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserLockedOutDomainEvent(Guid UserId, DateTimeOffset LockoutEnd) : DomainEvent;
