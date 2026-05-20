using ShopIt.Framework.Domain.Events;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Domain event raised when a Tenant is activated.
/// </summary>
public class TenantActivatedDomainEvent(Guid tenantId) : IDomainEvent
{
    public Guid TenantId { get; } = tenantId;
}
