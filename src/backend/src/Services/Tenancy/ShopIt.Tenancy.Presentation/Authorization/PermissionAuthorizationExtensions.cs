using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Presentation.Authorization;

namespace ShopIt.Tenancy.Presentation.Authorization;

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers the handler that resolves <see cref="PermissionRequirement"/>s against the
    /// Identity service. Scoped because it depends on scoped services.
    /// </summary>
    /// <remarks>
    /// The requirement and the <c>RequirePermission</c> endpoint convention are shared from
    /// <see cref="ShopIt.Framework.Presentation.Authorization"/>. Only this registration is
    /// service-specific, because it names this service's handler.
    /// </remarks>
    public static IServiceCollection AddTenantPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, TenantPermissionAuthorizationHandler>();
        return services;
    }
}
