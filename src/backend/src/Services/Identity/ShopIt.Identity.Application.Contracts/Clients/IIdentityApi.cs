using Refit;
using ShopIt.Identity.Application.Contracts.Models;

namespace ShopIt.Identity.Application.Contracts.Clients;

/// <summary>
/// Refit interface for calling the Identity service's internal API.
/// </summary>
[Headers("Accept: application/json")]
public interface IIdentityApi
{
    [Post("/api/internal/validate-credentials")]
    Task<ApiResponse<CredentialValidationResponse>> ValidateCredentialsAsync([Body] CredentialValidationRequest request);
}
