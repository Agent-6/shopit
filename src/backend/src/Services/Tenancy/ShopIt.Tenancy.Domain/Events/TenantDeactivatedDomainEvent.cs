using ShopIt.Framework.Domain.Events;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Domain event raised when a Tenant is deactivated.
/// </summary>
public record TenantDeactivatedDomainEvent(Guid tenantId) : DomainEvent;
