using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection, including Kafka-based
    /// integration event infrastructure (Outbox + Inbox).
    /// </summary>
    /// <param name="services">The service collection to add the persistence services to.</param>
    /// <param name="databaseName">The name of the database connection string in configuration. Use the database resource name when using aspire.</param>
    /// <param name="configuration">The configuration object to retrieve connection strings from.</param>
    /// <param name="configureOutbox">Optional action to configure outbox options.</param>
    /// <param name="configureInbox">Optional action to configure inbox options.</param>
    /// <param name="handlerAssemblies">Additional assemblies to scan for integration event handlers.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string databaseName,
        IConfiguration configuration,
        Action<OutboxOptions>? configureOutbox = null,
        Action<InboxOptions>? configureInbox = null,
        params Assembly[] handlerAssemblies)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(databaseName);
            options.UseNpgsql(connectionString);
        });

        services.AddPersistenceServices<ApplicationDbContext>(configuration, typeof(DependencyInjection).Assembly);

        // Wire up Kafka integration event infrastructure
        services.AddKafkaIntegration<ApplicationDbContext>(
            configuration,
            configureOutbox,
            configureInbox,
            [typeof(DependencyInjection).Assembly, .. handlerAssemblies]);

        return services;
    }
}
