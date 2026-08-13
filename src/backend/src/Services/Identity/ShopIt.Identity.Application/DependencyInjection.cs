using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core;
using ShopIt.Identity.Application.DataSeeding;
using ShopIt.Identity.Application.Permissions;
using ShopIt.Identity.Application.Users.Activation;
using ShopIt.Identity.Domain.Permissions;
using ShopIt.Identity.Domain.Roles;

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

        // Built-in role definitions (name + default permission set), used by the seeders.
        services.AddSingleton<IRoleDefinitionProvider, ShopItIdentityRoleDefinitionProvider>();

        // Provisioning of tenant roles + admin user (triggered by tenant creation).
        services.AddScoped<ITenantDataSeeder, TenantDataSeeder>();

        // Resolves a user's effective permissions (used by /users/me/permissions and the
        // permission authorization handler). Scoped because it depends on scoped managers.
        services.AddScoped<IPermissionResolver, PermissionResolver>();

        return services;
    }
}
