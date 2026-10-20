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
    /// <param name="consumerGroupId">
    /// This service's Kafka consumer group. Must be unique per service: a group id shared with another
    /// service means each of them receives only part of a topic's messages. Required, so it cannot be
    /// forgotten and cannot silently fall back to a shared default.
    /// </param>
    /// <param name="inbox">
    /// What this service consumes. Required and explicit: <see cref="InboxSubscription.None"/> for a
    /// publish-only service, or <see cref="InboxSubscription.Consuming"/> with its topics. A service
    /// cannot end up with an inbox that starts and silently consumes nothing.
    /// </param>
    /// <param name="configuration">Optional configuration to read <c>ConnectionStrings:kafka</c> from.</param>
    /// <param name="configureOutbox">Optional action to configure <see cref="OutboxOptions"/>.</param>
    /// <param name="configureInbox">Optional action to configure non-topic <see cref="InboxOptions"/> settings.</param>
    /// <param name="assemblies">Assemblies to scan for <see cref="IIntegrationEventHandler{TEvent}"/> implementations.</param>
    /// <returns>The updated service collection.</returns>
    /// <exception cref="InvalidOperationException">
    /// A subscribed topic has no registered handler — the message would be consumed and dropped.
    /// </exception>
    public static IServiceCollection AddKafkaIntegration<TContext>(
        this IServiceCollection services,
        string consumerGroupId,
        InboxSubscription inbox,
        IConfiguration? configuration = null,
        Action<OutboxOptions>? configureOutbox = null,
        Action<InboxOptions>? configureInbox = null,
        params Assembly[] assemblies)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroupId);
        ArgumentNullException.ThrowIfNull(inbox);

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
        // Assigned after the callback so the parameters are authoritative: the group id and the topic
        // list are properties of the service's declaration, not something an options action can vary.
        inboxOptions.ConsumerGroupId = consumerGroupId;
        inboxOptions.Topics = [.. inbox.Topics];
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(inboxOptions));

        // Register background services. The inbox processor only exists for a service that declared
        // consumption; "publish-only" is then visible in the graph, not inferred from an empty list at
        // runtime.
        services.AddHostedService<OutboxProcessor<TContext>>();
        if (inbox.IsConsuming)
        {
            services.AddHostedService<InboxProcessor<TContext>>();
        }

        // Scan and register IIntegrationEventHandler<> implementations
        services.AddIntegrationEventHandlers(assemblies);

        // The registry is built from those handlers, so "what this service can dispatch" is exactly
        // "what it has handlers for" — no assembly scanning at dispatch time.
        var registry = BuildInboxEventRegistry(services, inbox, consumerGroupId);
        services.AddSingleton(registry);

        return services;
    }

    /// <summary>
    /// Builds the inbox's event-type lookup from the registered handlers and checks the declared
    /// subscriptions against it.
    /// </summary>
    /// <remarks>
    /// A subscribed event with no handler would be consumed, recorded and then dropped — a healthy-looking
    /// service that never reacts. Here that is a startup error naming the event and the consumer group.
    /// </remarks>
    private static InboxEventRegistry BuildInboxEventRegistry(
        IServiceCollection services,
        InboxSubscription inbox,
        string consumerGroupId)
    {
        var handledEventTypes = services
            .Where(descriptor => descriptor.ServiceType.IsGenericType
                && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
            .Select(descriptor => descriptor.ServiceType.GetGenericArguments()[0])
            .Distinct()
            .ToList();

        var registry = new InboxEventRegistry(handledEventTypes);

        if (!inbox.IsConsuming)
        {
            return registry;
        }

        var unhandled = inbox.Topics.Where(topic => !registry.TryResolve(topic, out _)).ToList();

        if (unhandled.Count > 0)
        {
            throw new InvalidOperationException(
                $"Consumer group '{consumerGroupId}' subscribes to {string.Join(", ", unhandled.Select(t => $"'{t}'"))} " +
                "but no IIntegrationEventHandler<T> is registered for that event. " +
                "Pass the assembly that contains the handler in handlerAssemblies, or remove the event " +
                "from the inbox subscription.");
        }

        return registry;
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
