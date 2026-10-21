using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Identity.Client.Services;

namespace ShopIt.Catalog.Presentation.Authorization;

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> by looking up the caller's effective permissions (via
/// the Identity service) and checking the required permission. Also rejects requests without an
/// interactive user (e.g. client-credentials tokens).
/// </summary>
/// <remarks>
/// <para>
/// The permission names it checks are this service's own — <c>catalog.view</c> / <c>catalog.write</c>,
/// declared in <c>ShopIt.Catalog.Domain.Permissions</c> — but they are <em>resolved</em> by Identity,
/// which owns the grants. That split is why the requirement is shared from the framework and only the
/// handler is per service.
/// </para>
/// <para>
/// A handler that returned <c>false</c> rather than failing silently would leave the requirement unmet and
/// produce a 403 anyway; <see cref="AuthorizationHandlerContext.Fail"/> is used so the reason is explicit
/// and cannot be overridden by another handler succeeding (ASP.NET Core lets one handler fail the whole
/// requirement).
/// </para>
/// </remarks>
public class CatalogPermissionAuthorizationHandler(
    IIdentityPermissionClient permissionClient) : AuthorizationHandler<PermissionRequirement>
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

        var permissions = await permissionClient.GetPermissionsAsync(userId);

        if (permissions.Grants(requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
