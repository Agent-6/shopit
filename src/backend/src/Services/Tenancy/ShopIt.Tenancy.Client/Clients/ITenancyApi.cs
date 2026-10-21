using Refit;
using ShopIt.Tenancy.Client.Models;

namespace ShopIt.Tenancy.Client.Clients;

/// <summary>
/// Refit interface for the Tenancy service's internal API.
/// </summary>
/// <remarks>
/// <para>
/// Only what another service must ask synchronously lives here. Tenant lifecycle and permission-catalogue changes
/// are events, not calls — the opposite direction — so this surface stays as small as the one question it answers:
/// which currency does this tenant price in.
/// </para>
/// <para>
/// It is called with a client-credentials token (see the catalog's Infrastructure), and Tenancy guards the group
/// with the same internal policy Identity uses, so a user token cannot reach it.
/// </para>
/// </remarks>
[Headers("Accept: application/json")]
public interface ITenancyApi
{
    /// <summary>
    /// The currency a tenant's new prices are denominated in.
    /// </summary>
    /// <param name="tenantId">The tenant to ask about.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The currency, or a <c>404</c> problem when the tenant does not exist in the registry — which is a real
    /// answer, not a transport failure, and is what tells a caller it must refuse the write.
    /// </returns>
    [Get("/api/internal/tenants/{tenantId}/currency")]
    Task<ApiResponse<TenantCurrencyResponse>> GetTenantCurrencyAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
