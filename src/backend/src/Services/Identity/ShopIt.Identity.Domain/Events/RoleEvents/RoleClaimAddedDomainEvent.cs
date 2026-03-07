using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Events.RoleEvents;

public class RoleClaimAddedDomainEvent(Guid roleId, string claimType, string claimValue) : IDomainEvent
{
    public Guid RoleId { get; private set; } = roleId;
    public string ClaimType { get; private set; } = claimType;
    public string ClaimValue { get; private set; } = claimValue;
}
