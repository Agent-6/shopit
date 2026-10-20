using ShopIt.Framework.Domain.Permissions;
using ShopIt.Framework.Persistence.Permissions;

namespace ShopIt.Framework.Persistence.Permissions;

/// <summary>
/// Publishes a service's permission catalog. Registered by
/// <c>AddPermissionCatalogPublishing</c>; invoked once per process start by
/// <see cref="PermissionCatalogPublisherHostedService{TProvider}"/>.
/// </summary>
/// <remarks>
/// The interface exists so the startup hook stays out of the composition root: a service
/// registers the hook and says nothing else about publishing. It is also the entry point for
/// an explicit publish, which <see cref="PermissionCatalogPublisher{TProvider}"/> collapses
/// into the same single run as the startup call.
/// </remarks>
public interface IPermissionCatalogPublisher
{
    /// <summary>
    /// Publishes the catalog if it has not been published yet in this process; otherwise
    /// does nothing. Never publishes twice, so a service that both registers the startup
    /// hook and calls this explicitly still publishes exactly once.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(CancellationToken cancellationToken = default);
}
