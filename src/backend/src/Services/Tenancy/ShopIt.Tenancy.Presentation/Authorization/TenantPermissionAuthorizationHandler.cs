using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Identity.Client.Services;

namespace ShopIt.Tenancy.Presentation.Authorization;

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> by looking up the caller's effective
/// permissions (via the Identity service) and checking the required permission. Also rejects
/// requests without an interactive user (e.g. client-credentials tokens).
/// </summary>
/// <remarks>
/// The tenant the request is acting as travels with the call, because Identity cannot recover it: the call
/// to Identity carries service credentials, so it has no tenant claim of its own, and a tenant user's
/// role-derived grants come from that tenant's copies of their roles (#86).
/// </remarks>
public class TenantPermissionAuthorizationHandler(
    IIdentityPermissionClient permissionClient,
    ICurrentTenant currentTenant) : AuthorizationHandler<PermissionRequirement>
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

        var permissions = await permissionClient.GetPermissionsAsync(userId, currentTenant.Id);

        if (permissions.Grants(requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
