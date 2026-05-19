using ShopIt.Framework.Domain.Events;

namespace ShopIt.Identity.Domain.Events.UserEvents;

public class UserClaimRemovedDomainEvent(Guid userId, string claimType, string claimValue) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public string ClaimType { get; } = claimType;
    public string ClaimValue { get; } = claimValue;
}
