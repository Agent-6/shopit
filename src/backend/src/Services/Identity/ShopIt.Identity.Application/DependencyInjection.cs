using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Application.Users.Activation;
using ShopIt.Identity.Domain.Permissions;

namespace ShopIt.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplicationServices(typeof(DependencyInjection).Assembly);

        // Self-contained, time-limited activation tokens (IDataProtector based).
        services.AddSingleton<IActivationTokenProvider, ActivationTokenProvider>();

        // Permission catalog is static and shared across requests.
        services.AddSingleton<IPermissionDefinitionProvider, ShopItIdentityPermissionDefinitionProvider>();

        // Resolves a user's effective permissions (used by /users/me/permissions and the
        // permission authorization handler). Scoped because it depends on scoped managers.
        services.AddScoped<IPermissionResolver, PermissionResolver>();

        return services;
    }
}
