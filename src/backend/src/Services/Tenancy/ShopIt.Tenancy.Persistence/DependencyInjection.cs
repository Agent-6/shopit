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
        InboxSubscription inbox,
        Action<OutboxOptions>? configureOutbox = null)
    {
        services.AddDbContext<TenancyDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(databaseName);
            options.UseNpgsql(connectionString);
        });

        services.AddPersistenceServices<TenancyDbContext>(configuration, typeof(DependencyInjection).Assembly);

        // The demo tenant Identity's demo users belong to (see TenantDataSeeder). Registered here because it writes
        // through this context; the composition root decides when to run it — after migrations, at startup.
        services.AddScoped<TenantDataSeeder>();

        // Wire up Kafka integration event infrastructure
        services.AddKafkaIntegration<TenancyDbContext>(
            "tenancy-inbox",
            inbox,
            configuration,
            configureOutbox,
            assemblies: [typeof(DependencyInjection).Assembly]);

        return services;
    }
}
