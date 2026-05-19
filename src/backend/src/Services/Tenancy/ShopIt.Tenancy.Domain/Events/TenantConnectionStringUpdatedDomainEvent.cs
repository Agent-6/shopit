using ShopIt.Framework.Domain.Events;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Domain event raised when a Tenant's database connection string is updated.
/// </summary>
public class TenantConnectionStringUpdatedDomainEvent(Guid tenantId, string? connectionString, Guid updatedBy) : IDomainEvent
{
    public Guid TenantId { get; } = tenantId;
    public string? ConnectionString { get; } = connectionString;
    public Guid UpdatedBy { get; } = updatedBy;
}
