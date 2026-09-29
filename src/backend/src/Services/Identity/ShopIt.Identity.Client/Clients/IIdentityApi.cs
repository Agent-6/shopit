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
    /// The caller's effective permissions, resolved and filtered by multi-tenancy side.
    /// </summary>
    /// <remarks>
    /// The client wrapper reads the shared cache before calling this, and Identity itself serves it
    /// from the same cache — so on a hit this endpoint is not reached at all.
    /// </remarks>
    [Get("/api/internal/users/{userId}/permissions")]
    Task<ApiResponse<UserPermissionsResponse>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously completes the invitation flow (token + password). The browser waits
    /// for the result so the Authentication service can sign the user in right away.
    /// </summary>
    [Post("/api/internal/activate-user")]
    Task<ApiResponse<ActivateUserResponse>> ActivateUserAsync([Body] ActivateUserRequest request);
}
