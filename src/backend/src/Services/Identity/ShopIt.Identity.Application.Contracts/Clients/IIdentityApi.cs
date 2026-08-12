using Refit;
using ShopIt.Identity.Application.Contracts.Models;

namespace ShopIt.Identity.Application.Contracts.Clients;

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
}
