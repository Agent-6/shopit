using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Persistence.Permissions;

/// <summary>
/// Consumes <see cref="PermissionCatalogPublishedIntegrationEvent"/> published by other
/// microservices (each service announces its permission catalog on startup / whenever its
/// permissions change). Upserts the definitions into the persisted catalog and grants any
/// new permissions to the Admin role — so a service can add permissions without redeploying
/// the Identity project.
/// </summary>
public class PermissionCatalogPublishedIntegrationEventHandler(
    IPermissionCatalogSynchronizer synchronizer,
    ILogger<PermissionCatalogPublishedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<PermissionCatalogPublishedIntegrationEvent>
{
    private readonly IPermissionCatalogSynchronizer _synchronizer = synchronizer;
    private readonly ILogger<PermissionCatalogPublishedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(
        PermissionCatalogPublishedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var groups = integrationEvent.Groups
            .Select(g => new PermissionGroupDefinition(
                new PermissionGroupName(g.Name),
                g.DisplayName,
                g.Permissions
                    .Select(p => new PermissionDefinition(
                        new PermissionName(p.Name),
                        p.DisplayName,
                        p.Description,
                        p.MultiTenancySide))
                    .ToList()))
            .ToList();

        _logger.LogInformation(
            "Received permission catalog from '{SourceService}' with {GroupCount} group(s).",
            integrationEvent.SourceService, groups.Count);

        await _synchronizer.SynchronizeAsync(integrationEvent.SourceService, groups, cancellationToken);
    }
}
