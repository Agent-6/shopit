using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserEmailConfirmedDomainEvent(Guid userId, string Email) : DomainEvent;
