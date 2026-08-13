using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserActivatedDomainEvent(Guid UserId, string Email) : DomainEvent;
