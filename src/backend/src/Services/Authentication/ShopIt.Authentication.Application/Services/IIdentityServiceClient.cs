using System.Threading.Tasks;
using ShopIt.Authentication.Application.Models;

namespace ShopIt.Authentication.Application.Services;

public interface IIdentityServiceClient
{
    Task<CredentialValidationResult?> ValidateCredentialsAsync(CredentialValidationRequest request);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}
