using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public class RoleClaimRemovedDomainEvent(Guid roleId, string claimType, string claimValue) : IDomainEvent
{
    public Guid RoleId { get; } = roleId;
    public string ClaimType { get; } = claimType;
    public string ClaimValue { get; } = claimValue;
}
