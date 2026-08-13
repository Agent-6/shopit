using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Application.DataSeeding;

namespace ShopIt.Identity.Application.IntegrationEvents;

/// <summary>
/// Consumes <see cref="TenantCreatedIntegrationEvent"/> from the Tenancy service and seeds
/// the tenant's static roles plus an admin user assigned to every role.
/// </summary>
public class TenantCreatedIntegrationEventHandler(
    ITenantDataSeeder tenantDataSeeder,
    ILogger<TenantCreatedIntegrationEventHandler> logger) : IIntegrationEventHandler<TenantCreatedIntegrationEvent>
{
    public async Task HandleAsync(TenantCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Provisioning tenant {TenantId} ({Name})...",
            integrationEvent.TenantId, integrationEvent.TenantName);

        await tenantDataSeeder.SeedTenantAsync(
            integrationEvent.TenantId,
            integrationEvent.TenantName,
            cancellationToken);
    }
}
