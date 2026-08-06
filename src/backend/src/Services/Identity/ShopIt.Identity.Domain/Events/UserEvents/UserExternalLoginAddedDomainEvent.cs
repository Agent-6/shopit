using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserExternalLoginAddedDomainEvent(Guid userId, string loginProvider, string providerKey) : DomainEvent;
