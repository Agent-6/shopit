using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserExternalLoginRemovedDomainEvent(Guid UserId, string LoginProvider, string ProviderKey) : DomainEvent;
