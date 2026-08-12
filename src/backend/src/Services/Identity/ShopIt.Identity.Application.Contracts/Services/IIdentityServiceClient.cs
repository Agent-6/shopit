using ShopIt.Identity.Application.Contracts.Models;

namespace ShopIt.Identity.Application.Contracts.Services;

public interface IIdentityServiceClient
{
    /// <summary>
    /// Validates a username/password pair against the Identity service during login.
    /// Returns <c>null</c> when the credentials are invalid or the request was not authorized.
    /// </summary>
    Task<CredentialValidationResponse?> ValidateCredentialsAsync(CredentialValidationRequest request);
}
