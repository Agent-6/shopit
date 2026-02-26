using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the persistence services to.</param>
    /// <param name="databaseName">The name of the database connection string in configuration. Use the database resource name when using aspire.</param>
    /// <param name="configuration">The configuration object to retrieve connection strings from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string databaseName, IConfiguration configuration, params Assembly[] assemblies)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(databaseName);
            options.UseNpgsql(connectionString);
        });

        services.AddPersistenceServices<AppDbContext>(configuration, assemblies);

        return services;
    }
}
