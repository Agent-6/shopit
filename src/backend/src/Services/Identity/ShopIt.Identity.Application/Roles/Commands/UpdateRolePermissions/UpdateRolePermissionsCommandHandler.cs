using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Permissions;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRolePermissions;

/// <summary>
/// Grants and revokes permission claims on a role. Only claims whose type is in the
/// permission catalog are touched, so custom (non-permission) claims are preserved —
/// unlike the removed raw role-claims endpoint, which replaced the entire claim set.
/// </summary>
public class UpdateRolePermissionsCommandHandler(
    RoleManager<Role> roleManager,
    IPermissionDefinitionProvider permissionCatalog) : ICommandHandler<UpdateRolePermissionsCommand, UpdateRolePermissionsResult>
{
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly IPermissionDefinitionProvider _permissionCatalog = permissionCatalog;

    public async Task<UpdateRolePermissionsResult> HandleAsync(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null) throw new KeyNotFoundException("Role not found");

        var knownPermissions = _permissionCatalog.GetAll()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var granted = new List<string>();
        var revoked = new List<string>();

        var existingClaims = (await _roleManager.GetClaimsAsync(role)).ToList();

        foreach (var p in request.Permissions)
        {
            // Ignore unknown permission names so a stale client cannot invent claims.
            if (!knownPermissions.Contains(p.PermissionName))
            {
                continue;
            }

            var existing = existingClaims.FirstOrDefault(c => c.Type == p.PermissionName);
            if (p.IsGranted && existing is null)
            {
                var res = await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(p.PermissionName, "true"));
                if (res.Succeeded) granted.Add(p.PermissionName);
            }
            else if (!p.IsGranted && existing is not null)
            {
                var res = await _roleManager.RemoveClaimAsync(role, existing);
                if (res.Succeeded) revoked.Add(p.PermissionName);
            }
        }

        return new UpdateRolePermissionsResult(role.Id, granted, revoked, DateTime.UtcNow);
    }
}
