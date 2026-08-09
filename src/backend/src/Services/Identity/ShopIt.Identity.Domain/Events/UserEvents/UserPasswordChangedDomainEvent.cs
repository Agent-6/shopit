using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserPasswordChangedDomainEvent(Guid UserId, string SecurityStamp) : DomainEvent;
