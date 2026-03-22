namespace ShopIt.Identity.Domain.Tenancy;

/// <summary>
/// Defines a contract for entities that are associated with a tenant.
/// </summary>
/// <remarks>Implement this interface to configure the entity for multi-tenancy.</remarks>
public interface ITenantEntity
{
    Guid TenantId { get; }
}
