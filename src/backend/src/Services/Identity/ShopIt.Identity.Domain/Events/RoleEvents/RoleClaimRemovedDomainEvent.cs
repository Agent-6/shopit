using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public record RoleClaimRemovedDomainEvent(Guid roleId, string claimType, string claimValue) : DomainEvent;
