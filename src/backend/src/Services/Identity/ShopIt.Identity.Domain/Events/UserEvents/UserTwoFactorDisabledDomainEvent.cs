using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserTwoFactorDisabledDomainEvent(Guid userId) : DomainEvent;
