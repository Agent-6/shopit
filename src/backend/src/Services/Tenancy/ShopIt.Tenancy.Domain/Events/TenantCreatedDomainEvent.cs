using ShopIt.Framework.Domain.Events;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Domain event raised when a new Tenant is created.
/// </summary>
public class TenantCreatedDomainEvent(Tenant tenant) : IDomainEvent
{
    public Tenant Tenant { get; } = tenant;
}
