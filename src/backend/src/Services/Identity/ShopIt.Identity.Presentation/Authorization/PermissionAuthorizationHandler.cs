using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Presentation.Authorization;

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> by loading the authenticated user from the
/// Identity database (via the <c>sub</c> claim) and checking their effective permissions
/// through <see cref="IPermissionResolver"/> (direct user claims + role claims, including
/// host system roles assigned to tenant users).
/// </summary>
public class PermissionAuthorizationHandler(
    UserManager<User> userManager,
    IPermissionResolver permissionResolver) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // OpenIddict access tokens use "sub"; tolerate ClaimTypes.NameIdentifier as well.
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User.FindFirstValue("sub");

        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            context.Fail(); // Not a request from an interactive user.
            return;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            context.Fail(); // Unknown or deactivated user — no permissions are granted.
            return;
        }

        var permissions = await permissionResolver.GetGrantedPermissionsAsync(user);
        if (permissions.Contains(requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
