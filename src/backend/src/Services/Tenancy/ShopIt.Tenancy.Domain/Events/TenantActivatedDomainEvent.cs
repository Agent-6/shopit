using ShopIt.Framework.Domain.Events;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Domain event raised when a Tenant is activated.
/// </summary>
public record TenantActivatedDomainEvent(Guid tenantId) : DomainEvent;
