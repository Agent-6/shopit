using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserSuspendedDomainEvent(Guid UserId, DateTime SuspendedUntil, string Reason) : DomainEvent;
