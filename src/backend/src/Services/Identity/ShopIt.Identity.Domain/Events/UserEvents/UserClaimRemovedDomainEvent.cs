using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserClaimRemovedDomainEvent(Guid userId, string claimType, string claimValue) : DomainEvent;
