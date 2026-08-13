using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Domain.Entities;

public class RoleClaim : IdentityRoleClaim<Guid>, IEntity<int>, ITenantEntity
{
    public Guid TenantId { get; private set; } = default!;

    // Public parameterless constructor for Identity
    public RoleClaim() : base() { }

    public static RoleClaim Create(Role role, string claimType, string claimValue)
    {
        var roleClaim = new RoleClaim()
        {
            RoleId = role.Id,
            ClaimType = claimType,
            ClaimValue = claimValue,
            TenantId = role.TenantId,
        };

        return roleClaim;
    }
}
