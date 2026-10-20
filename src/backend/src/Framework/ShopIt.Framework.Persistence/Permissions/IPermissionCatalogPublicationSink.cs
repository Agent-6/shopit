using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Persistence.Permissions;

/// <summary>
/// Receives a service's permission catalog at startup and does something with it. The
/// catalog is delivered once per process start by
/// <see cref="PermissionCatalogPublisher{TProvider}"/>, after the database migrations have run.
/// </summary>
/// <remarks>
/// <para>
/// Two sinks exist because two services own the catalog in different ways. A service that does
/// <em>not</em> own the permission registry publishes its catalog to the Identity service, which
/// is what <see cref="OutboxPermissionCatalogPublicationSink{TContext}"/> does — it writes a
/// <c>PermissionCatalogPublishedIntegrationEvent</c> to the outbox and commits it. The Identity
/// service <em>is</em> the registry, so it synchronizes its own catalog in place via
/// <c>SynchronizingPermissionCatalogPublicationSink</c> rather than sending itself a message.
/// </para>
/// <para>
/// Implementations are resolved from a fresh scope, so they may depend on scoped services such
/// as the <c>DbContext</c>. They must be idempotent: the hook runs on every startup.
/// </para>
/// </remarks>
public interface IPermissionCatalogPublicationSink
{
    /// <summary>
    /// Publishes <paramref name="groups"/> on behalf of <paramref name="sourceService"/>.
    /// </summary>
    /// <param name="sourceService">The name of the service the catalog belongs to.</param>
    /// <param name="groups">The service's permission groups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(
        string sourceService,
        IReadOnlyList<PermissionGroupDefinition> groups,
        CancellationToken cancellationToken = default);
}
