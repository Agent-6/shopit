namespace ShopIt.Tenancy.Client.Models;

/// <summary>
/// What Tenancy's internal <c>/api/internal/tenants/{tenantId}/currency</c> endpoint answers.
/// </summary>
/// <remarks>
/// The tenant id is echoed alongside the code so the response is self-describing: a caller resolving several
/// tenants (or reading a value out of a shared cache) cannot mistake one tenant's currency for another's, which is
/// exactly the cross-tenant mix-up ADR-0006's transport exists to rule out.
/// </remarks>
/// <param name="TenantId">The tenant the currency belongs to.</param>
/// <param name="Currency">Its ISO 4217 code, upper case.</param>
public record TenantCurrencyResponse(Guid TenantId, string Currency);
