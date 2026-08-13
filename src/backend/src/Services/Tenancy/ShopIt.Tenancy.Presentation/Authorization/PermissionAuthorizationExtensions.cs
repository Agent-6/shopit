using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ShopIt.Tenancy.Presentation.Authorization;

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers the handler that resolves <see cref="PermissionRequirement"/>s against the
    /// Identity service. Scoped because it depends on scoped services.
    /// </summary>
    public static IServiceCollection AddTenantPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, TenantPermissionAuthorizationHandler>();
        return services;
    }

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
