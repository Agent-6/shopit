using Refit;
using ShopIt.Identity.Application.Contracts.Clients;
using ShopIt.Identity.Application.Contracts.Models;
using ShopIt.Identity.Application.Contracts.Services;

namespace ShopIt.Identity.Application.Contracts.Implementations;

public class IdentityServiceClient(IIdentityApi identityApi) : IIdentityServiceClient
{
    private readonly IIdentityApi _identityApi = identityApi;

    public async Task<CredentialValidationResponse?> ValidateCredentialsAsync(CredentialValidationRequest request)
    {
        try
        {
            var response =  await _identityApi.ValidateCredentialsAsync(request);
            return response.IsSuccessful ? response.Content : null;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<ForgotPasswordResponse?> ForgotPasswordAsync(string email)
    {
        try
        {
            var response = await _identityApi.ForgotPasswordAsync(new ForgotPasswordRequest(email));
            return response.IsSuccessful ? response.Content : null;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            var response = await _identityApi.ResetPasswordAsync(new ResetPasswordRequest(email, token, newPassword));
            return response.IsSuccessful && response.Content;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return false;
        }
    }

    public async Task<SendEmailConfirmationOtpResponse?> SendEmailConfirmationOtpAsync(string email)
    {
        try
        {
            var response = await _identityApi.SendEmailConfirmationOtpAsync(new SendEmailConfirmationOtpRequest(email));
            return response.IsSuccessful ? response.Content : null;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<bool> ConfirmEmailAsync(string email, string code)
    {
        try
        {
            var response = await _identityApi.ConfirmEmailAsync(new ConfirmEmailRequest(email, code));
            return response.IsSuccessful && response.Content;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return false;
        }
    }
}
