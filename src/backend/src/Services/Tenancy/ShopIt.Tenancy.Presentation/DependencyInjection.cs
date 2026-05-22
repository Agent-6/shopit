using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Presentation;

namespace ShopIt.Tenancy.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPresentationServices(configuration, typeof(DependencyInjection).Assembly);
        return services;
    }
}
