using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Identity.Application.Contracts.Events;

/// <summary>
/// Published by the Tenancy service when a tenant is created, so the Identity service can
/// provision the tenant's static roles and admin user (tenant data seeding).
/// </summary>
/// <param name="RequestId">Correlation id (echoed from the originating request when event-driven).</param>
/// <param name="TenantId">The id of the newly created tenant.</param>
/// <param name="TenantName">The display name of the newly created tenant.</param>
public record TenantCreatedIntegrationEvent(
    Guid RequestId,
    Guid TenantId,
    string TenantName) : IntegrationEvent;
