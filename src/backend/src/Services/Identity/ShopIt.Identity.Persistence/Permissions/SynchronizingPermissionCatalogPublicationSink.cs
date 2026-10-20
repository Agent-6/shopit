using ShopIt.Framework.Domain.Permissions;
using ShopIt.Framework.Persistence.Permissions;

namespace ShopIt.Identity.Persistence.Permissions;

/// <summary>
/// Identity's permission-catalog sink: synchronizes its own catalog in place, because Identity
/// is the permission registry. Publishing to Kafka and consuming the message back would add a
/// broker round-trip to startup for no benefit, and would leave the catalog empty for the
/// window between startup and the inbox poll.
/// </summary>
/// <param name="synchronizer">The catalog synchronizer that upserts definitions and grants them to Admin.</param>
public sealed class SynchronizingPermissionCatalogPublicationSink(IPermissionCatalogSynchronizer synchronizer)
    : IPermissionCatalogPublicationSink
{
    /// <inheritdoc />
    public Task PublishAsync(
        string sourceService,
        IReadOnlyList<PermissionGroupDefinition> groups,
        CancellationToken cancellationToken = default) =>
        synchronizer.SynchronizeAsync(sourceService, groups, cancellationToken);
}
