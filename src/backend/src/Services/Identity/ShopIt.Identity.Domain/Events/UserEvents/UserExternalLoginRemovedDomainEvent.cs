using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserExternalLoginRemovedDomainEvent(Guid userId, string loginProvider, string providerKey) : DomainEvent;
