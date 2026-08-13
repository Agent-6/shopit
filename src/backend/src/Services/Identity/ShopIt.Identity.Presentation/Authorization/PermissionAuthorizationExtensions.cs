using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ShopIt.Identity.Presentation.Authorization;

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers the handler that resolves <see cref="PermissionRequirement"/>s from the Identity
    /// database. Registered as scoped because it depends on the scoped UserManager/RoleManager.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
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
    /// <c>app.MapPost("/", CreateUser).RequirePermission(ShopItIdentityPermissions.Users.Create)</c>.
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
