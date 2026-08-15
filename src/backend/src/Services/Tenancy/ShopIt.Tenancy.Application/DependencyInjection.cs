using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core;
using ShopIt.Tenancy.Domain.Permissions;

namespace ShopIt.Tenancy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplicationServices(typeof(DependencyInjection).Assembly);

        // This service's permission catalog (published to Identity at startup).
        services.AddSingleton<ShopItTenancyPermissionDefinitionProvider>();

        return services;
    }
}
