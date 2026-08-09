using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserClaimRemovedDomainEvent(Guid UserId, string ClaimType, string ClaimValue) : DomainEvent;
