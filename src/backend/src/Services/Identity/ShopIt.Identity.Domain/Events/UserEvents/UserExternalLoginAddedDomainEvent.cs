using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserExternalLoginAddedDomainEvent(Guid UserId, string LoginProvider, string ProviderKey) : DomainEvent;
