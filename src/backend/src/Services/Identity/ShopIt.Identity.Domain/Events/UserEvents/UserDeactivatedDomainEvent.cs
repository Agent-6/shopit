using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserDeactivatedDomainEvent(Guid userId, string reason) : DomainEvent;
