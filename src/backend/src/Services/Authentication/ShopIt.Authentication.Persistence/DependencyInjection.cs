using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Authentication.Persistence.Data;

namespace ShopIt.Authentication.Persistence;

/// <summary>
/// Adds persistence services to the Authentication service, including Kafka-based
/// integration event infrastructure (Outbox + Inbox).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection.
    /// </summary>
    /// <param name=\"services\">The service collection to add the persistence services to.</param>
    /// <param name=\"databaseName\">The name of the database connection string in configuration. Use the database resource name when using aspire.</param>
    /// <param name=\"configuration\">The configuration object to retrieve connection strings from.</param>
    /// <param name=\"configureOutbox\">Optional action to configure outbox options.</param>
    /// <param name="inbox">What this service consumes: <see cref="InboxSubscription.None"/> or <see cref="InboxSubscription.Consuming"/> with its topics.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string databaseName,
        IConfiguration configuration,
        InboxSubscription inbox,
        Action<OutboxOptions>? configureOutbox = null,
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
            "authentication-inbox",
            inbox,
            configuration,
            configureOutbox,
            assemblies: [typeof(DependencyInjection).Assembly, .. handlerAssemblies]);

        return services;
    }
}
