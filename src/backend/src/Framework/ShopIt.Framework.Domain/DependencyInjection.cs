using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Domain;

/// <summary>
/// Provides extension methods to register domain-level dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the core domain services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection with registered domain services.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// Uses the domain services.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The service provider with registered domain services.</returns>
    public static IServiceProvider UseDomainServices(this IServiceProvider serviceProvider)
    {
        var guidProvider = serviceProvider.GetRequiredService<IGuidProvider>();
        var dateProvider = serviceProvider.GetRequiredService<IDateProvider>();

        DomainProviders.SetProviders(guidProvider, dateProvider);

        return serviceProvider;
    }
}
