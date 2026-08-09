using ShopIt.Framework.Domain.Events;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Domain event raised when a new Tenant is created.
/// </summary>
public record TenantCreatedDomainEvent(Tenant Tenant) : DomainEvent;
