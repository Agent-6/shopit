using ShopIt.Identity.Application.Contracts.Models;

namespace ShopIt.Identity.Application.Contracts.Services;

public interface IIdentityServiceClient
{
    Task<CredentialValidationResponse?> ValidateCredentialsAsync(CredentialValidationRequest request);
    Task<ForgotPasswordResponse?> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    Task<SendEmailConfirmationOtpResponse?> SendEmailConfirmationOtpAsync(string email);
    Task<bool> ConfirmEmailAsync(string email, string code);
}
