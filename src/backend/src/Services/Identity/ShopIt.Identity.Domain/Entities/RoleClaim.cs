using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities;

public class RoleClaim : Entity<Guid>
{
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; }
    public string ClaimType { get; private set; }
    public string ClaimValue { get; private set; }
    public Guid? TenantId { get; private set; }

    private RoleClaim() : base() { }

    // TODO: add non-generic Entity for multi-keyed entities
    private RoleClaim(Guid id) : base(id) { }

    public static RoleClaim Create(Guid id, Role role, string claimType, string claimValue)
    {
        var roleClaim = new RoleClaim(id)
        {
            RoleId = role.Id,
            Role = role,
            ClaimType = claimType,
            ClaimValue = claimValue,
            TenantId = role.TenantId,
        };

        return roleClaim;
    }
}
