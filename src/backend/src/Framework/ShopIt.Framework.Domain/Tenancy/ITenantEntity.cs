using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Framework.Domain.Tenancy;

/// <summary>
/// Defines a contract for entities that are associated with a tenant.
/// </summary>
/// <remarks>
/// Implement this interface to have the entity tenant-scoped. A <c>DbContext</c> that applies the
/// tenant configuration attaches a query filter on <see cref="TenantId"/> to every implementor, so a
/// query made under tenant A can never return tenant B's rows.
/// <para>
/// Host-level tables — the tenant registry itself, system-wide catalogs — must <em>not</em> implement
/// this interface. Their absence of a filter is deliberate and should be stated rather than assumed.
/// </para>
/// See <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </remarks>
public interface ITenantEntity : IEntity
{
    Guid TenantId { get; }
}
