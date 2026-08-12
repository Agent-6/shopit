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

    [Post("/api/internal/forgot-password")]
    Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync([Body] ForgotPasswordRequest request);

    [Post("/api/internal/reset-password")]
    Task<ApiResponse<bool>> ResetPasswordAsync([Body] ResetPasswordRequest request);

    [Post("/api/internal/send-email-confirmation-otp")]
    Task<ApiResponse<SendEmailConfirmationOtpResponse>> SendEmailConfirmationOtpAsync([Body] SendEmailConfirmationOtpRequest request);

    [Post("/api/internal/confirm-email")]
    Task<ApiResponse<bool>> ConfirmEmailAsync([Body] ConfirmEmailRequest request);
}
