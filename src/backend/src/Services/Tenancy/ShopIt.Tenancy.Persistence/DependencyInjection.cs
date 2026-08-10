using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Tenancy.Persistence.Data;

namespace ShopIt.Tenancy.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection, including Kafka-based
    /// integration event infrastructure (Outbox + Inbox).
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string databaseName,
        IConfiguration configuration,
        Action<OutboxOptions>? configureOutbox = null,
        Action<InboxOptions>? configureInbox = null)
    {
        services.AddDbContext<TenancyDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(databaseName);
            options.UseNpgsql(connectionString);
        });

        services.AddPersistenceServices<TenancyDbContext>(configuration, typeof(DependencyInjection).Assembly);

        // Wire up Kafka integration event infrastructure
        services.AddKafkaIntegration<TenancyDbContext>(
            configuration,
            configureOutbox,
            configureInbox,
            typeof(DependencyInjection).Assembly);

        return services;
    }
}
