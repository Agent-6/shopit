using Microsoft.Extensions.Hosting;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Persistence.Permissions;

/// <summary>
/// Runs the permission-catalog publish hook once, as the service starts.
/// </summary>
/// <remarks>
/// A hosted service rather than a call in <c>Program.cs</c>: hosted services start before the
/// web server accepts requests, so the catalog is published before the first request can
/// authorize against it, and the composition root stays free of publishing logic.
/// Migrations still run first — each service migrates before <c>app.Run()</c>, and the host
/// only starts its hosted services after that returns.
/// </remarks>
/// <typeparam name="TProvider">The service's permission definition provider.</typeparam>
public sealed class PermissionCatalogPublisherHostedService<TProvider>(IPermissionCatalogPublisher publisher)
    : IHostedService
    where TProvider : class, IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => publisher.PublishAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
