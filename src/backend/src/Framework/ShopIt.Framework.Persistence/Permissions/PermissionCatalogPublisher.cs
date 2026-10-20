using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Persistence.Permissions;

/// <summary>
/// Publishes <typeparamref name="TProvider"/>'s catalog exactly once per process, through the
/// registered <see cref="IPermissionCatalogPublicationSink"/>.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a singleton, so the "published once" guard is process-wide even though the
/// provider and sink are resolved from a fresh scope per call. Publishing on every startup is
/// deliberate and safe: the Identity service's synchronizer upserts by permission name and
/// grants only permissions it has not already granted.
/// </para>
/// <para>
/// It lives here rather than in a service's <c>Program.cs</c> because the composition root
/// should only say "this service has a permission catalog", not know how catalogs travel.
/// </para>
/// </remarks>
/// <typeparam name="TProvider">The service's permission definition provider.</typeparam>
public sealed class PermissionCatalogPublisher<TProvider>(
    IServiceScopeFactory scopeFactory,
    PermissionCatalogPublishingOptions options,
    ILogger<PermissionCatalogPublisher<TProvider>> logger)
    : IPermissionCatalogPublisher
    where TProvider : class, IPermissionDefinitionProvider
{
    private int _published;

    /// <inheritdoc />
    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _published, 1) == 1)
        {
            logger.LogDebug(
                "Permission catalog from '{SourceService}' has already been published in this process; skipping.",
                options.SourceService);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<TProvider>();
            var sink = scope.ServiceProvider.GetRequiredService<IPermissionCatalogPublicationSink>();

            var groups = provider.GetGroups();

            await sink.PublishAsync(options.SourceService, groups, cancellationToken);

            logger.LogInformation(
                "Published permission catalog from '{SourceService}' ({GroupCount} group(s), {PermissionCount} permission(s)).",
                options.SourceService,
                groups.Count,
                groups.Sum(g => g.Permissions.Count));
        }
        catch
        {
            // Allow a retry (e.g. a transient database failure) rather than leaving the
            // process permanently thinking it succeeded.
            Interlocked.Exchange(ref _published, 0);
            throw;
        }
    }
}
