using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core;

namespace ShopIt.Tenancy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplicationServices(typeof(DependencyInjection).Assembly);
        return services;
    }
}
