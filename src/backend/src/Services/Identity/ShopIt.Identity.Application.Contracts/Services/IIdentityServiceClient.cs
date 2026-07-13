using ShopIt.Identity.Application.Contracts.Models;

namespace ShopIt.Identity.Application.Contracts.Services;

public interface IIdentityServiceClient
{
    Task<CredentialValidationResponse?> ValidateCredentialsAsync(CredentialValidationRequest request);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}
