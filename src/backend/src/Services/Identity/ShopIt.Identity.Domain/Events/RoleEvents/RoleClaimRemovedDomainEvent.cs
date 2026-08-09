using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public record RoleClaimRemovedDomainEvent(Guid RoleId, string ClaimType, string ClaimValue) : DomainEvent;
