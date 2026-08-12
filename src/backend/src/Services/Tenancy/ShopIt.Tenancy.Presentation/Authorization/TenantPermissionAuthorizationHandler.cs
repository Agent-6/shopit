using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ShopIt.Tenancy.Application.Permissions;

namespace ShopIt.Tenancy.Presentation.Authorization;

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> by looking up the caller's effective
/// permissions (via the Identity service) and checking the required permission. Also rejects
/// requests without an interactive user (e.g. client-credentials tokens).
/// </summary>
public class TenantPermissionAuthorizationHandler(
    ITenantPermissionClient permissionClient) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User.FindFirstValue("sub");

        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            context.Fail(); // Not a request from an interactive user.
            return;
        }

        var permissions = await permissionClient.GetGrantedPermissionsAsync(userId);
        if (permissions.Contains(requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
