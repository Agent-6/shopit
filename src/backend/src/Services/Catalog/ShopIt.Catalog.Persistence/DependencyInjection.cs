using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Catalog.Persistence.Data;

namespace ShopIt.Catalog.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection, including the Kafka-based
    /// integration event infrastructure (outbox, plus an inbox when this service consumes).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseName">The connection-string name the AppHost injects.</param>
    /// <param name="configuration">Configuration to read the connection string and Kafka bootstrap servers from.</param>
    /// <param name="inbox">What this service consumes — <see cref="InboxSubscription.None"/> while it is publish-only.</param>
    /// <param name="configureOutbox">Optional outbox tuning.</param>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string databaseName,
        IConfiguration configuration,
        InboxSubscription inbox,
        Action<OutboxOptions>? configureOutbox = null)
    {
        services.AddDbContext<CatalogDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(databaseName);
            options.UseNpgsql(connectionString);
        });

        services.AddPersistenceServices<CatalogDbContext>(configuration, typeof(DependencyInjection).Assembly);

        // The demo tenant's sample catalog (two categories, three products). Registered here because it
        // writes through this context inside its own tenant scope; the composition root decides when to run
        // it — after migrations, at startup.
        services.AddScoped<CatalogDataSeeder>();

        // Kafka integration event infrastructure. The consumer group is this service's own and is
        // named here rather than defaulted: sharing a group with another service splits each topic's
        // partitions between them, silently, with no error.
        services.AddKafkaIntegration<CatalogDbContext>(
            "catalog-inbox",
            inbox,
            configuration,
            configureOutbox,
            assemblies: [typeof(DependencyInjection).Assembly]);

        return services;
    }
}
