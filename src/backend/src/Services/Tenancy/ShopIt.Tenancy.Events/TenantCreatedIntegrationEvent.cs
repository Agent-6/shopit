using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Tenancy.Events;

/// <summary>
/// Published by the Tenancy service when a tenant is created, so the Identity service can
/// provision the tenant's static roles and admin user (tenant data seeding).
/// </summary>
/// <remarks>
/// Owned by Tenancy because Tenancy owns the meaning of the message — it is the tenant registry, so
/// "a tenant was created" is its domain event. Consumers reference this assembly; this assembly
/// references only <c>ShopIt.Framework.Core</c> and nothing else, which is the point of a per-service
/// events contract.
/// </remarks>
/// <param name="RequestId">Correlation id (echoed from the originating request when event-driven).</param>
/// <param name="TenantId">The id of the newly created tenant.</param>
/// <param name="TenantName">The display name of the newly created tenant.</param>
public record TenantCreatedIntegrationEvent(
    Guid RequestId,
    Guid TenantId,
    string TenantName) : IntegrationEvent;
