using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserEmailChangedDomainEvent(Guid UserId, string NewEmail) : DomainEvent;
