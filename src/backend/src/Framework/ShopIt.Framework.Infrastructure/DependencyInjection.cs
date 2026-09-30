using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopIt.Framework.Application.Caching;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Events;
using ShopIt.Framework.Domain.Providers;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Domain.Users;
using ShopIt.Framework.Infrastructure.Caching;
using ShopIt.Framework.Infrastructure.Events;
using ShopIt.Framework.Infrastructure.Providers;
using ShopIt.Framework.Infrastructure.Tenancy;
using ShopIt.Framework.Infrastructure.Users;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace ShopIt.Framework.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the specified service collection using the provided configuration.
    /// </summary>
    /// <param name="services">The service collection to which the infrastructure services will be added.</param>
    /// <param name="configuration">The application configuration used to configure the infrastructure services.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Request-scoped context -- the acting tenant and the acting user -- is HTTP-backed, so
        // every ASP.NET service gets it from here rather than registering its own.
        // See docs/adr/0001-tenant-isolation-model.md.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddSingleton<IDateProvider, DateProvider>();
        services.AddSingleton<IGuidProvider, GuidProvider>();

        AddCaching(services, configuration);

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // OutboxWriter depends on the scoped DbContext registered by each service's persistence layer.
        // The service's AddPersistence call must register a DbContext before this is resolved.
        services.AddScoped<IOutboxWriter>(sp =>
        {
            var dbContext = sp.GetRequiredService<DbContext>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutboxWriter>>();
            return new OutboxWriter(dbContext, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers the shared cache: an in-process L1 in front of Redis as L2, with Redis also acting
    /// as the backplane that propagates invalidations between processes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The backplane is the whole point. Removing a key without one clears only this process's L1 and
    /// the shared L2 — other processes keep serving their own stale L1 entry until it expires, which
    /// would defeat invalidation entirely.
    /// </para>
    /// <para>
    /// If no <c>cache</c> connection string is configured the cache degrades to L1-only rather than
    /// throwing, so a service can still start without Redis. That is a real degradation, not a no-op:
    /// each process then has its own view and invalidation is local only.
    /// </para>
    /// </remarks>
    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var fusionCache = services.AddFusionCache()
            // Required for the distributed cache: FusionCache does not bundle a serializer, and without
            // one it throws at first use rather than at registration -- which is how this was found.
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(5),
                JitterMaxDuration = TimeSpan.FromSeconds(2),
                // If Redis is unreachable, serve the last known value rather than failing the request.
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(2),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
                AllowBackgroundDistributedCacheOperations = true,
            });

        var redisConnection = configuration.GetConnectionString("cache");

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            fusionCache
                .WithDistributedCache(new RedisCache(new RedisCacheOptions { Configuration = redisConnection }))
                .WithBackplane(new RedisBackplane(new RedisBackplaneOptions { Configuration = redisConnection }));
        }

        // Stateless and thread-safe; FusionCache is a singleton and this holds nothing else.
        services.AddSingleton<ICache, FusionCacheAdapter>();
    }
}
