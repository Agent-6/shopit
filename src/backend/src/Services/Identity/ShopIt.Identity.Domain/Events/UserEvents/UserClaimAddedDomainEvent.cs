using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public record UserClaimAddedDomainEvent(Guid userId, string claimType, string claimValue) : DomainEvent;
