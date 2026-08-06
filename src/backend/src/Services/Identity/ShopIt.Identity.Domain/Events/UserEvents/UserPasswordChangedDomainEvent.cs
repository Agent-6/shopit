using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserPasswordChangedDomainEvent(Guid userId, string securityStamp) : DomainEvent;
