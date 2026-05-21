using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Domain.Providers;
using ShopIt.Framework.Infrastructure.Providers;

namespace ShopIt.Framework.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the specified service collection using the provided configuration.
    /// </summary>
    /// <param name="services">The service collection to which the infrastructure services will be added.</param>
    /// <param name="configuration">The application configuration used to configure the infrastructure services.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateProvider, DateProvider>();
        services.AddSingleton<IGuidProvider, GuidProvider>();

        return services;
    }
}
