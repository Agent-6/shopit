namespace ShopIt.Framework.Domain.Tenancy;

/// <summary>
/// The tenant the current scope is acting as.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Change"/> is the <em>only</em> supported way to change tenant scope. It sets the ambient
/// tenant, which moves the query filter <em>and</em> <see cref="ITenantEntity.TenantId"/> stamping on new
/// rows together — so scope and write-stamping cannot drift apart.
/// </para>
/// <para>
/// Host scope — every tenant visible — is <c>Change(new TenantInfo(Guid.Empty, "Host"))</c>. Because
/// <see cref="Id"/> is <see cref="Guid.Empty"/> at host scope, the filter is disabled; entering host
/// scope must therefore always be deliberate.
/// </para>
/// <para>
/// Bypassing the filter instead (EF Core's <c>IgnoreQueryFilters()</c>) is not a supported mechanism: it
/// leaves ambient scope and stamping untouched, so a row read across tenants can be written back under
/// the acting tenant.
/// </para>
/// See <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </remarks>
public interface ICurrentTenant
{
    /// <summary>The acting tenant, or <see cref="Guid.Empty"/> at host scope.</summary>
    Guid Id { get; }

    /// <summary>The acting tenant's name, or <c>"Host"</c> at host scope.</summary>
    string? Name { get; }

    /// <summary>Whether the scope is the host — that is, no tenant is being acted as.</summary>
    bool IsHost => Id == Guid.Empty;

    /// <summary>
    /// Acts as <paramref name="tenant"/> until the returned handle is disposed, then restores the
    /// previous scope. Nesting is supported and unwinds in reverse order.
    /// </summary>
    IDisposable Change(TenantInfo tenant);
}
