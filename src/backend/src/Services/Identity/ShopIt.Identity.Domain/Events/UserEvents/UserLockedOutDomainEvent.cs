using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserLockedOutDomainEvent(Guid userId, DateTimeOffset lockoutEnd) : DomainEvent;
