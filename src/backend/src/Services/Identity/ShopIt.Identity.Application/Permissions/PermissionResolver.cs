using Microsoft.AspNetCore.Identity;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;

namespace ShopIt.Identity.Application.Permissions;

public class PermissionResolver(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ICurrentTenant currentTenant) : IPermissionResolver
{
    public async Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(User user, CancellationToken cancellationToken = default)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Direct permission claims on the user (e.g. set via the permissions editor).
        foreach (var claim in await userManager.GetClaimsAsync(user))
        {
            permissions.Add(claim.Type);
        }

        await AddRolePermissionsAsync(user, permissions);

        // System (host) roles assigned to a tenant user are invisible to the tenant-scoped
        // role queries above, so retry with host scope (tenant filter off).
        if (!currentTenant.IsHost)
        {
            using (currentTenant.Change(new TenantInfo(Guid.Empty, "Host")))
            {
                await AddRolePermissionsAsync(user, permissions);
            }
        }

        return permissions;
    }

    private async Task AddRolePermissionsAsync(User user, ISet<string> permissions)
    {
        var roleNames = await userManager.GetRolesAsync(user);
        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName!);
            if (role is null)
            {
                continue;
            }

            foreach (var claim in await roleManager.GetClaimsAsync(role))
            {
                permissions.Add(claim.Type);
            }
        }
    }
}
