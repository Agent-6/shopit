using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Permissions;

namespace ShopIt.Identity.Application.Permissions.Queries.GetPermissionMatrix;

public class GetPermissionMatrixQueryHandler(
    RoleManager<Role> roleManager,
    IPermissionDefinitionProvider permissionCatalog) : IQueryHandler<GetPermissionMatrixQuery, GetPermissionMatrixResult>
{
    public async Task<GetPermissionMatrixResult> HandleAsync(
        GetPermissionMatrixQuery request,
        CancellationToken cancellationToken)
    {
        var groups = permissionCatalog.GetGroups()
            .Select(g => new PermissionMatrixGroupItem(
                g.Name,
                g.DisplayName,
                g.Permissions
                    .Select(p => new PermissionMatrixDefinitionItem(p.Name, p.DisplayName, p.Description))
                    .ToList()))
            .ToList();

        // RoleManager.Roles is tenant-filtered via the DbContext, matching the other queries.
        var roles = new List<PermissionMatrixRoleItem>();
        foreach (var role in roleManager.Roles.OrderBy(r => r.Name).ToList())
        {
            var claims = await roleManager.GetClaimsAsync(role);
            roles.Add(new PermissionMatrixRoleItem(
                role.Id,
                role.Name ?? string.Empty,
                claims.Select(c => new PermissionMatrixClaimItem(c.Type, c.Value)).ToList()));
        }

        return new GetPermissionMatrixResult(groups, roles);
    }
}
