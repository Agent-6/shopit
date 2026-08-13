using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Stores;

/// <summary>
/// Role store that keeps role lookups tenant-scoped and stamps created claims with the role's tenant.
/// </summary>
/// <remarks>
/// Role names are unique per tenant (see <c>RoleConfiguration</c>), so the default store's
/// global <c>FindByNameAsync</c> becomes ambiguous at host scope where the tenant query filter
/// is bypassed and same-named host/tenant roles coexist. This store scopes name lookups to the
/// ambient tenant. It also stamps <see cref="RoleClaim.TenantId"/> from the owning role, since
/// the default store leaves it as <see cref="Guid.Empty"/>.
/// </remarks>
public class TenantRoleStore(
    ApplicationDbContext context,
    ICurrentTenant currentTenant) : RoleStore<Role, ApplicationDbContext, Guid, UserRole, RoleClaim>(context)
{
    /// <summary>
    /// Finds a role by normalized name within the ambient tenant, so a name lookup at host
    /// scope resolves the host copy rather than any same-named tenant role.
    /// </summary>
    public override Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var tenantId = currentTenant.Id;
        return Roles.FirstOrDefaultAsync(
            r => r.NormalizedName == normalizedRoleName && r.TenantId == tenantId,
            cancellationToken);
    }

    /// <summary>
    /// Creates role claims with the owning role's tenant id so they remain visible to
    /// tenant-scoped queries (and don't collide under the per-role unique index).
    /// </summary>
    protected override RoleClaim CreateRoleClaim(Role role, Claim claim)
        => RoleClaim.Create(role, claim.Type, claim.Value);
}
