using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public record RoleClaimAddedDomainEvent(Guid RoleId, string ClaimType, string ClaimValue) : DomainEvent;
