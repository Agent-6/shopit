using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Notifications.Persistence.Data;

namespace ShopIt.Notifications.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection, including the Kafka-based
    /// integration event inbox (the Notifications service is a consumer-only sink, so no
    /// outbox writes are expected — the table is still created for the outbox processor).
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string databaseName,
        IConfiguration configuration,
        Action<OutboxOptions>? configureOutbox = null,
        Action<InboxOptions>? configureInbox = null,
        params Assembly[] handlerAssemblies)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(databaseName);
            options.UseNpgsql(connectionString);
        });

        services.AddPersistenceServices<NotificationsDbContext>(configuration, typeof(DependencyInjection).Assembly);

        // Wire up Kafka integration event infrastructure
        services.AddKafkaIntegration<NotificationsDbContext>(
            configuration,
            configureOutbox,
            configureInbox,
            [typeof(DependencyInjection).Assembly, .. handlerAssemblies]);

        return services;
    }
}
