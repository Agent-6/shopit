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
        services.AddSingleton<IDateProvider, DateProvider>();
        services.AddSingleton<IGuidProvider, GuidProvider>();

        return services;
    }
}
