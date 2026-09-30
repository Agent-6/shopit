using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Roles;

namespace ShopIt.Identity.Application.Permissions;

public class PermissionResolver(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ICurrentTenant currentTenant,
    IPermissionDefinitionProvider permissionCatalog,
    IRoleDefinitionProvider roleDefinitions) : IPermissionResolver
{
    private static readonly IReadOnlySet<string> None = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public async Task<EffectivePermissions> GetEffectivePermissionsAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await ResolveRolesAsync(user);

        // A built-in role whose definition grants everything is represented by the flag rather than by
        // enumerating the catalog. That is what makes this value independent of the catalog's contents,
        // so republishing a catalog invalidates nothing.
        if (roles.Any(GrantsAllPermissions))
        {
            return new EffectivePermissions(IsAllPermissions: true, None);
        }

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Direct permission claims on the user (e.g. set via the permissions editor).
        foreach (var claim in await userManager.GetClaimsAsync(user))
        {
            permissions.Add(claim.Type);
        }

        foreach (var role in roles)
        {
            foreach (var claim in await roleManager.GetClaimsAsync(role))
            {
                permissions.Add(claim.Type);
            }
        }

        return new EffectivePermissions(IsAllPermissions: false, FilterBySide(user, permissions));
    }

    public async Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(User user, CancellationToken cancellationToken = default)
    {
        var effective = await GetEffectivePermissionsAsync(user, cancellationToken);

        if (!effective.IsAllPermissions)
        {
            return effective.Permissions;
        }

        // Materialised only here, for callers that genuinely need the list — the permissions UI, and
        // the seeding paths that write default grants.
        return permissionCatalog.GetAll()
            .Where(p => p.MultiTenancySide.IsAvailableOn(SideOf(user)))
            .Select(p => p.Name.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlySet<string> FilterBySide(User user, IReadOnlySet<string> permissions)
    {
        // A permission is only effective on the side it is available on: filter out grants for
        // permissions that don't apply to the user's own tenant side.
        var userSide = SideOf(user);

        var sideByPermission = permissionCatalog.GetAll()
            .GroupBy(p => p.Name.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().MultiTenancySide, StringComparer.OrdinalIgnoreCase);

        return permissions
            .Where(name => !sideByPermission.TryGetValue(name, out var side) || side.IsAvailableOn(userSide))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static PermissionMultiTenancySide SideOf(User user) =>
        user.TenantId == Guid.Empty
            ? PermissionMultiTenancySide.Host
            : PermissionMultiTenancySide.Tenant;

    /// <summary>
    /// The roles the user holds, including host roles assigned to a tenant user — those are invisible to
    /// tenant-scoped role queries and need host scope to resolve.
    /// </summary>
    private async Task<List<Role>> ResolveRolesAsync(User user)
    {
        var roles = new List<Role>();

        await CollectRolesAsync(user, roles);

        if (!currentTenant.IsHost)
        {
            using (currentTenant.Change(new TenantInfo(Guid.Empty, "Host")))
            {
                await CollectRolesAsync(user, roles);
            }
        }

        return roles;
    }

    private async Task CollectRolesAsync(User user, List<Role> roles)
    {
        foreach (var roleName in await userManager.GetRolesAsync(user))
        {
            if (roleName is null)
            {
                continue;
            }

            var role = await roleManager.FindByNameAsync(roleName);
            if (role is not null && roles.All(r => r.Id != role.Id))
            {
                roles.Add(role);
            }
        }
    }

    /// <summary>
    /// Whether this role is built-in <em>and</em> its definition grants every permission — Admin, in the
    /// shipped definitions.
    /// </summary>
    /// <remarks>
    /// Both halves are load-bearing. <see cref="Role.IsStatic"/> proves the role came from a definition
    /// rather than from a user, whose chosen name cannot be trusted for anything security-relevant
    /// (see #50); <see cref="RoleDefinition.GrantsAllPermissions"/> is the definition actually saying so.
    /// Matching on the name alone would let a caller with <c>role.create</c> obtain every permission by
    /// naming a role "Admin".
    /// </remarks>
    private bool GrantsAllPermissions(Role role)
    {
        if (!role.IsStatic || role.Name is null)
        {
            return false;
        }

        var definition = roleDefinitions.GetAll().FirstOrDefault(d =>
            string.Equals(d.Name.Value, role.Name, StringComparison.OrdinalIgnoreCase));

        return definition?.GrantsAllPermissions == true;
    }
}
