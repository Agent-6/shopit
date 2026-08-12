using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Infrastructure;

namespace ShopIt.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructureServices(configuration);
        return services;
    }
}
