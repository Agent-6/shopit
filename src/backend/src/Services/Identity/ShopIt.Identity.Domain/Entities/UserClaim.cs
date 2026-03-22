using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Domain.Entities;

public class UserClaim : IdentityUserClaim<Guid>, IEntity<int>, ITenantEntity
{
    public Guid TenantId { get; private set; } = default!;

    // Public parameterless constructor for Identity
    public UserClaim() : base() { }

    internal static UserClaim Create(User user, string claimType, string claimValue)
    {
        var userClaim = new UserClaim()
        {
            UserId = user.Id,
            ClaimType = claimType,
            ClaimValue = claimValue,
            TenantId = user.TenantId
        };

        return userClaim;
    }
}
