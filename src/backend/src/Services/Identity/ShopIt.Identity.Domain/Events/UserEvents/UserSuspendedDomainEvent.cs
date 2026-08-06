using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserSuspendedDomainEvent(Guid userId, DateTime suspendedUntil, string reason) : DomainEvent;
