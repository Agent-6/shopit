using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Core.UnitOfWork;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Framework.Domain.Repositories;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Framework.Persistence.Permissions;
using ShopIt.Framework.Persistence.UnitOfWork;

namespace ShopIt.Framework.Persistence;

/// <summary>
/// Helper to read Kafka bootstrap servers from Aspire-injected environment variables
/// (<c>KAFKA_HOST</c> and <c>KAFKA_PORT</c>) or fall back to configuration / defaults.
/// </summary>
public static class KafkaConfiguration
{
    /// <summary>
    /// Reads the Kafka bootstrap servers from:
    /// 1. <c>KAFKA_HOST</c> + <c>KAFKA_PORT</c> environment variables (Aspire injection)
    /// 2. <c>ConnectionStrings:kafka</c> configuration (manual setup)
    /// 3. Default <c>localhost:9092</c>
    /// </summary>
    public static string GetBootstrapServers(IConfiguration? configuration = null)
    {
        var host = Environment.GetEnvironmentVariable("KAFKA_HOST");
        var port = Environment.GetEnvironmentVariable("KAFKA_PORT");

        if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
            return $"{host}:{port}";

        var connStr = configuration?.GetConnectionString("kafka");
        if (!string.IsNullOrEmpty(connStr))
            return connStr.Replace("kafka://", string.Empty);

        return "localhost:9092";
    }
}

public static class DependencyInjection
{
    /// <summary>
    /// Adds the persistence services to the service collection.
    /// <list type="bullet">
    /// <item><description>Registers the unit of work implementation for the given DbContext type.</description></item>
    /// <item><description>Registers the repository implementations in the given assemblies.</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext to be used.</typeparam>
    /// <param name="services">The service collection to add the persistence services to.</param>
    /// <param name="configuration">The configuration object to retrieve connection strings from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPersistenceServices<TContext>(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
        where TContext : DbContext
    {
        // EF Core's AddDbContext registers only the concrete context type. Bridge it to
        // the non-generic DbContext base so framework services that resolve DbContext
        // (e.g. IOutboxWriter registered by AddInfrastructureServices) work with any
        // context type.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddRepositories(assemblies);
        return services;
    }

    /// <summary>
    /// Adds the Kafka-based Integration Event infrastructure (Outbox + Inbox processors)
    /// for the specified DbContext type. Reads Kafka bootstrap servers from environment
    /// variables (Aspire <c>KAFKA_HOST</c> / <c>KAFKA_PORT</c>) or falls back to
    /// <c>ConnectionStrings:kafka</c> configuration.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type for the service. Outbox and Inbox tables must be in its schema.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration to read <c>ConnectionStrings:kafka</c> from.</param>
    /// <param name="configureOutbox">Optional action to configure <see cref="OutboxOptions"/>.</param>
    /// <param name="configureInbox">Optional action to configure <see cref="InboxOptions"/>.</param>
    /// <param name="assemblies">Assemblies to scan for <see cref="IIntegrationEventHandler{TEvent}"/> implementations.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddKafkaIntegration<TContext>(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<OutboxOptions>? configureOutbox = null,
        Action<InboxOptions>? configureInbox = null,
        params Assembly[] assemblies)
        where TContext : DbContext
    {
        var bootstrapServers = KafkaConfiguration.GetBootstrapServers(configuration);

        // Configure outbox options
        var outboxOptions = new OutboxOptions();
        outboxOptions.KafkaBootstrapServers = bootstrapServers;
        configureOutbox?.Invoke(outboxOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(outboxOptions));

        // Configure inbox options
        var inboxOptions = new InboxOptions();
        inboxOptions.KafkaBootstrapServers = bootstrapServers;
        configureInbox?.Invoke(inboxOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(inboxOptions));

        // Register background services
        services.AddHostedService<OutboxProcessor<TContext>>();
        services.AddHostedService<InboxProcessor<TContext>>();

        // Scan and register IIntegrationEventHandler<> implementations
        services.AddIntegrationEventHandlers(assemblies);

        return services;
    }

    /// <summary>
    /// Registers the permission-catalog publishing hook for a service that does not own the
    /// permission registry: on startup, the service's <typeparamref name="TProvider"/> groups are
    /// written to the outbox as <paramref name="createEvent"/>'s integration event, and the
    /// Identity service consumes them like any other message.
    /// <list type="bullet">
    /// <item><description>Publishes exactly once per process, from a hosted service that runs before the web server accepts requests.</description></item>
    /// <item><description>Requires the service to have registered the outbox (see <see cref="AddKafkaIntegration{TContext}"/>).</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="TProvider">The service's <see cref="IPermissionDefinitionProvider"/> implementation.</typeparam>
    /// <typeparam name="TContext">The service's DbContext, which owns the outbox table.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="sourceService">The name stamped on the catalog entries (e.g. <c>"Tenancy"</c>).</param>
    /// <param name="createEvent">Maps (source service, groups) to the catalog integration event.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPermissionCatalogPublishing<TProvider, TContext>(
        this IServiceCollection services,
        string sourceService,
        Func<string, IReadOnlyList<PermissionGroupDefinition>, IntegrationEvent> createEvent)
        where TProvider : class, IPermissionDefinitionProvider
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceService);
        ArgumentNullException.ThrowIfNull(createEvent);

        services.TryAddSingleton<TProvider>();
        services.AddSingleton(new PermissionCatalogPublishingOptions
        {
            SourceService = sourceService,
            CreateEvent = createEvent,
        });

        return services.AddPermissionCatalogPublishingHook<TProvider, OutboxPermissionCatalogPublicationSink<TContext>>();
    }

    /// <summary>
    /// Registers the permission-catalog publishing hook for a service that owns the permission
    /// registry and therefore applies its catalog locally instead of publishing it over Kafka.
    /// <para>
    /// The service supplies <typeparamref name="TSink"/>, which receives the catalog once per
    /// process start; the Identity service's sink synchronizes the persisted catalog directly.
    /// </para>
    /// </summary>
    /// <typeparam name="TSink">The service's <see cref="IPermissionCatalogPublicationSink"/> implementation.</typeparam>
    /// <typeparam name="TProvider">The service's <see cref="IPermissionDefinitionProvider"/> implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="sourceService">The name stamped on the catalog entries (e.g. <c>"Identity"</c>).</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddPermissionCatalogPublishing<TSink, TProvider>(
        this IServiceCollection services,
        string sourceService)
        where TSink : class, IPermissionCatalogPublicationSink
        where TProvider : class, IPermissionDefinitionProvider
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceService);

        services.TryAddSingleton<TProvider>();
        services.AddSingleton(new PermissionCatalogPublishingOptions { SourceService = sourceService });

        return services.AddPermissionCatalogPublishingHook<TProvider, TSink>();
    }

    /// <summary>
    /// Registers the sink, publisher and startup hook shared by both
    /// <c>AddPermissionCatalogPublishing</c> overloads.
    /// </summary>
    private static IServiceCollection AddPermissionCatalogPublishingHook<TProvider, TSink>(this IServiceCollection services)
        where TProvider : class, IPermissionDefinitionProvider
        where TSink : class, IPermissionCatalogPublicationSink
    {
        services.TryAddScoped<IPermissionCatalogPublicationSink, TSink>();
        services.TryAddSingleton<PermissionCatalogPublisher<TProvider>>();
        services.TryAddSingleton<IPermissionCatalogPublisher>(sp => sp.GetRequiredService<PermissionCatalogPublisher<TProvider>>());
        services.AddHostedService<PermissionCatalogPublisherHostedService<TProvider>>();

        return services;
    }

    /// <summary>
    /// Adds repository implementations to the service collection by scanning the specified assemblies for classes that implement the IRepository&lt;,&gt; interface.
    /// </summary>
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

    /// <summary>
    /// Scans the provided assemblies for <see cref="IIntegrationEventHandler{TEvent}"/> implementations
    /// and registers them as scoped services.
    /// </summary>
    private static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            return services;

        services.Scan(scan =>
            scan.FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        return services;
    }
}
