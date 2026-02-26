using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Core.UnitOfWork;
using ShopIt.Framework.Domain.Repositories;
using ShopIt.Framework.Persistence.UnitOfWork;

namespace ShopIt.Framework.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection.
    /// <list type="bullet">
    /// <item><description>Registers the unit of work implementation for the given DbContext type.</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext to be used.</typeparam>
    /// <param name="services">The service collection to add the persistence services to.</param>
    /// <param name="configuration">The configuration object to retrieve connection strings from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPersistenceServices<TContext>(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddRepositories(assemblies);
        return services;
    }

    /// <summary>
    /// Adds repository implementations to the service collection by scanning the specified assemblies for classes that implement the IRepository<,> interface.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    private static IServiceCollection AddRepositories(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register all repository implementations in the specified assemblies
        services.Scan(scan =>
            scan.FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IRepository<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        return services;
    }
}
