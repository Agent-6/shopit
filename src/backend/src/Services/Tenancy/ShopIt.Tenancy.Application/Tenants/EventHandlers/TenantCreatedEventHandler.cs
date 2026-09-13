using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Events;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Tenancy.Domain.Events;

namespace ShopIt.Tenancy.Application.Tenants.EventHandlers;

/// <summary>
/// Publishes a <see cref="TenantCreatedIntegrationEvent"/> into the outbox when a tenant is
/// created, so the Identity service can provision the tenant's static roles and admin user.
/// Runs inside the create-tenant transaction (via the UnitOfWork domain-event dispatch),
/// so the outbox write is atomic with the tenant row.
/// </summary>
public class TenantCreatedEventHandler(
    IOutboxWriter outboxWriter,
    ILogger<TenantCreatedEventHandler> logger) : IDomainEventHandler<TenantCreatedDomainEvent>
{
    public async Task HandleAsync(TenantCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var tenant = domainEvent.Tenant;

        logger.LogInformation(
            "Tenant {TenantId} ({Name}) created — publishing provisioning event.",
            tenant.Id, tenant.Name);

        await outboxWriter.WriteAsync(
            new TenantCreatedIntegrationEvent(Guid.NewGuid(), tenant.Id, tenant.Name),
            cancellationToken);
    }
}
