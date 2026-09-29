using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Presentation.Authorization;

namespace ShopIt.Identity.Presentation.Authorization;

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers the handler that resolves <see cref="PermissionRequirement"/>s from the Identity
    /// database. Registered as scoped because it depends on the scoped UserManager/RoleManager.
    /// </summary>
    /// <remarks>
    /// The requirement and the <c>RequirePermission</c> endpoint convention are shared from
    /// <see cref="ShopIt.Framework.Presentation.Authorization"/>. Only this registration is
    /// service-specific, because it names this service's handler.
    /// </remarks>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
