using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserClaimAddedDomainEvent(Guid UserId, string ClaimType, string ClaimValue) : DomainEvent;
