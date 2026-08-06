using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public record RoleClaimAddedDomainEvent(Guid roleId, string claimType, string claimValue) : DomainEvent;
