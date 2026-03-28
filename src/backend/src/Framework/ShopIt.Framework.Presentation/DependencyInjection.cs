using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Presentation.Modules;

namespace ShopIt.Framework.Presentation;

public static class DependencyInjection
{
    /// <summary>
    /// Adds presentation layer services and modules to the specified service collection using the provided
    /// configuration and assemblies.
    /// </summary>
    /// <param name="services">The service collection to which the presentation services and modules will be added.</param>
    /// <param name="configuration">The application configuration used to configure the presentation services.</param>
    /// <param name="assemblies">The assemblies to scan for modules to register with the service collection.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
    {
        services.AddModules(assemblies);
        return services;
    }

    /// <summary>
    /// Registers all implementations of the <see cref="EndpointsModule"/> class found in the specified assemblies as singleton services.
    /// </summary>
    /// <remarks>This method is intended to be used during application startup to enable modular endpoint
    /// registration.</remarks>
    /// <param name="services">The service collection to which the modules will be added.</param>
    /// <param name="assemblies">An array of assemblies to scan for <see cref="EndpointsModule"/> implementations.</param>
    /// <returns>The same IServiceCollection instance so that additional calls can be chained.</returns>
    private static IServiceCollection AddModules(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register all module implementations in the specified assemblies
        services.Scan(scan =>
            scan.FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<EndpointsModule>())
                .As<EndpointsModule>()
                .WithSingletonLifetime()
        );

        return services;
    }

    /// <summary>
    /// Registers all endpoints defined by discovered endpoint modules with the specified route builder.
    /// </summary>
    /// <remarks>This method locates all registered implementations of <see cref="EndpointsModule"/> from the
    /// application's service provider and invokes their endpoint mapping logic. Use this method during application
    /// startup to ensure all modular endpoints are mapped.</remarks>
    /// <param name="app">The endpoint route builder to which endpoints will be mapped.</param>
    /// <returns>The same instance of <paramref name="app"/> to allow for method chaining.</returns>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var modules = app.ServiceProvider.GetRequiredService<IEnumerable<EndpointsModule>>();
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}
