using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Stores;

/// <summary>
/// User store whose role-name lookups are scoped to the user's tenant and which stamps
/// tenant ids on the rows it creates (role joins, user claims).
/// </summary>
/// <remarks>
/// Role names are unique per tenant (see <c>RoleConfiguration</c>), so the default store's
/// role-name lookups become ambiguous at host scope where the tenant query filter is bypassed
/// (e.g. the seed's <c>IsInRoleAsync</c> crashed with "Sequence contains more than one element").
/// This store resolves role names within the user's tenant, falling back to the host copy, and
/// stamps <see cref="UserRole.TenantId"/>/<see cref="UserClaim.TenantId"/> from the user, which
/// the default store leaves as <see cref="Guid.Empty"/>.
/// </remarks>
public class TenantUserStore(ApplicationDbContext context)
    : UserStore<User, Role, ApplicationDbContext, Guid, UserClaim, UserRole, UserLogin, UserToken, RoleClaim>(context)
{
    /// <inheritdoc />
    public override async Task<bool> IsInRoleAsync(User user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(normalizedRoleName);

        // The user may be joined to the tenant copy or the host copy of the role name.
        foreach (var role in await ResolveRolesForUserAsync(user, normalizedRoleName, cancellationToken))
        {
            var userRole = await FindUserRoleAsync(user.Id, role.Id, cancellationToken);
            if (userRole is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public override async Task AddToRoleAsync(User user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(normalizedRoleName);

        var roles = await ResolveRolesForUserAsync(user, normalizedRoleName, cancellationToken);
        if (roles.Count == 0)
        {
            return;
        }

        // No-op when the user already holds a role with this name (either copy).
        foreach (var role in roles)
        {
            var userRole = await FindUserRoleAsync(user.Id, role.Id, cancellationToken);
            if (userRole is not null)
            {
                return;
            }
        }

        // Assign the copy that belongs to the user's tenant, falling back to the host copy.
        var target = roles[0];
        var newUserRole = CreateUserRole(user, target);
        await Context.Set<UserRole>().AddAsync(newUserRole, cancellationToken);
        await SaveChanges(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task RemoveFromRoleAsync(User user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(normalizedRoleName);

        var removed = false;

        // Remove joins to every candidate copy (tenant and host), so a user holding the
        // host copy of a role name is not left with a dangling membership.
        foreach (var role in await ResolveRolesForUserAsync(user, normalizedRoleName, cancellationToken))
        {
            var userRole = await FindUserRoleAsync(user.Id, role.Id, cancellationToken);
            if (userRole is not null)
            {
                Context.Set<UserRole>().Remove(userRole);
                removed = true;
            }
        }

        if (removed)
        {
            await SaveChanges(cancellationToken);
        }
    }

    /// <summary>
    /// Creates role joins stamped with the user's tenant.
    /// </summary>
    protected override UserRole CreateUserRole(User user, Role role)
        => UserRole.Create(user, role);

    /// <summary>
    /// Creates user claims stamped with the user's tenant.
    /// </summary>
    protected override UserClaim CreateUserClaim(User user, Claim claim)
        => UserClaim.Create(user, claim.Type, claim.Value);

    /// <summary>
    /// Resolves the roles matching a normalized name for the given user: the copy in the
    /// user's tenant first, then the host copy (host roles may be assigned to tenant users).
    /// </summary>
    private async Task<IReadOnlyList<Role>> ResolveRolesForUserAsync(User user, string normalizedRoleName, CancellationToken cancellationToken)
    {
        var roles = new List<Role>(2);

        var tenantCopy = await Context.Set<Role>().FirstOrDefaultAsync(
            r => r.NormalizedName == normalizedRoleName && r.TenantId == user.TenantId,
            cancellationToken);
        if (tenantCopy is not null)
        {
            roles.Add(tenantCopy);
        }

        if (user.TenantId != Guid.Empty)
        {
            var hostCopy = await Context.Set<Role>().FirstOrDefaultAsync(
                r => r.NormalizedName == normalizedRoleName && r.TenantId == Guid.Empty,
                cancellationToken);
            if (hostCopy is not null)
            {
                roles.Add(hostCopy);
            }
        }

        return roles;
    }
}
