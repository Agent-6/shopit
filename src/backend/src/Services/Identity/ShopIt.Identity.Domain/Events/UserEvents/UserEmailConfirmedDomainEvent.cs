using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserEmailConfirmedDomainEvent(Guid UserId, string? Email) : DomainEvent;
