using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserLoggedInDomainEvent(Guid UserId, string LoginProvider) : DomainEvent;
