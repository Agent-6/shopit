using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Tenancy.Persistence.Data;

namespace ShopIt.Tenancy.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string databaseName, IConfiguration configuration)
    {
        services.AddDbContext<TenancyDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(databaseName);
            options.UseNpgsql(connectionString);
        });

        services.AddPersistenceServices<TenancyDbContext>(configuration, typeof(DependencyInjection).Assembly);

        return services;
    }
}
