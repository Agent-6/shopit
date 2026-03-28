using Microsoft.AspNetCore.Identity;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Application.Users.Queries.GetUserPermissions;

public class GetUserPermissionsQueryHandler : IQueryHandler<GetUserPermissionsQuery, GetUserPermissionsResult>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public GetUserPermissionsQueryHandler(UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<GetUserPermissionsResult> HandleAsync(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new KeyNotFoundException("User not found");

        var permissions = new List<UserPermissionItem>();
        var inherited = new List<InheritedPermissionItem>();

        var claims = await _userManager.GetClaimsAsync(user);
        foreach (var c in claims)
        {
            permissions.Add(new UserPermissionItem(c.Type, true, "Direct"));
        }

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName!);
            if (role is null) continue;

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var rc in roleClaims)
            {
                permissions.Add(new UserPermissionItem(rc.Type, true, "Role"));
                inherited.Add(new InheritedPermissionItem(rc.Type, role.Name!));
            }
        }

        return new GetUserPermissionsResult(request.UserId, permissions, inherited);
    }
}
