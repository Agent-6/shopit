using ShopIt.Identity.Application.Contracts.Models;

namespace ShopIt.Identity.Application.Contracts.Services;

public interface IIdentityServiceClient
{
    /// <summary>
    /// Validates a username/password pair against the Identity service during login.
    /// Returns <c>null</c> when the credentials are invalid or the request was not authorized.
    /// </summary>
    Task<CredentialValidationResponse?> ValidateCredentialsAsync(CredentialValidationRequest request);

    /// <summary>
    /// Completes the invitation activation flow (validates the token, stores the password and
    /// activates the account). Returns <c>null</c> when the Identity service could not be reached.
    /// </summary>
    Task<ActivateUserResponse?> ActivateUserAsync(ActivateUserRequest request);
}
