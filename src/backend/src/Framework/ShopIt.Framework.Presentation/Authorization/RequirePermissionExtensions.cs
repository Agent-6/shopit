using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace ShopIt.Framework.Presentation.Authorization;

/// <summary>
/// Endpoint conventions for requiring a permission.
/// </summary>
/// <remarks>
/// The <see cref="IAuthorizationHandler"/> that resolves <see cref="PermissionRequirement"/> is
/// registered by each service, because resolution differs per service. This type holds only what is
/// genuinely identical everywhere.
/// </remarks>
public static class RequirePermissionExtensions
{
    /// <summary>
    /// Adds a <see cref="PermissionRequirement"/> to the policy being built.
    /// </summary>
    public static AuthorizationPolicyBuilder RequirePermission(
        this AuthorizationPolicyBuilder builder,
        string permissionName)
    {
        return builder.AddRequirements(new PermissionRequirement(permissionName));
    }

    /// <summary>
    /// Requires the authenticated user to hold the given permission, e.g.
    /// <c>app.MapGet("/", GetTenants).RequirePermission(ShopItTenancyPermissions.View)</c>.
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissionName)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequirePermission(permissionName);
        });
    }
}
