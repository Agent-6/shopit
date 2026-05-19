using ShopIt.Framework.Domain.Events;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Domain event raised when a Tenant's basic details are updated.
/// </summary>
public class TenantUpdatedDomainEvent(Tenant tenant) : IDomainEvent
{
    public Tenant Tenant { get; } = tenant;
}
