using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserEmailChangedDomainEvent(Guid userId, string newEmail) : DomainEvent;
