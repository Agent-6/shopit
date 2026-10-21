using Refit;
using ShopIt.Identity.Client.Models;

namespace ShopIt.Identity.Client.Clients;

/// <summary>
/// Refit interface for calling the Identity service's internal API.
/// Only request/response operations that cannot be event-driven live here —
/// everything else is communicated through Kafka integration events.
/// </summary>
[Headers("Accept: application/json")]
public interface IIdentityApi
{
    /// <summary>
    /// Synchronously validates credentials during login. This operation is
    /// interactive (the browser waits for the result) and therefore stays HTTP.
    /// </summary>
    [Post("/api/internal/validate-credentials")]
    Task<ApiResponse<CredentialValidationResponse>> ValidateCredentialsAsync([Body] CredentialValidationRequest request);

    /// <summary>
    /// The user's effective permissions <em>in a tenant</em>, resolved and filtered by multi-tenancy side.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tenant is required, not inferred: this call is made with client credentials, so the request
    /// carries no tenant claim and Identity would otherwise resolve the user's roles in host scope — where
    /// a tenant user's role-derived grants do not exist (#86). It is the tenant the <em>user</em> is being
    /// authorised in, which for a request is the caller's own tenant from their token.
    /// </para>
    /// <para>
    /// The client wrapper reads the shared cache before calling this, and Identity itself serves it from
    /// the same cache, keyed by tenant and user — so on a hit this endpoint is not reached at all.
    /// </para>
    /// </remarks>
    /// <param name="userId">The user whose permissions are needed.</param>
    /// <param name="tenantId">The tenant to resolve them in; <see cref="Guid.Empty"/> for host scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/api/internal/users/{userId}/permissions")]
    Task<ApiResponse<UserPermissionsResponse>> GetUserPermissionsAsync(
        Guid userId,
        [Query] Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously completes the invitation flow (token + password). The browser waits
    /// for the result so the Authentication service can sign the user in right away.
    /// </summary>
    [Post("/api/internal/activate-user")]
    Task<ApiResponse<ActivateUserResponse>> ActivateUserAsync([Body] ActivateUserRequest request);
}
