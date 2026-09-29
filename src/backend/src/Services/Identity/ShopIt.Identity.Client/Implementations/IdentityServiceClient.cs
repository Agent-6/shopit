using Refit;
using ShopIt.Identity.Client.Clients;
using ShopIt.Identity.Client.Models;
using ShopIt.Identity.Client.Services;

namespace ShopIt.Identity.Client.Implementations;

public class IdentityServiceClient(IIdentityApi identityApi) : IIdentityServiceClient
{
    private readonly IIdentityApi _identityApi = identityApi;

    public async Task<CredentialValidationResponse?> ValidateCredentialsAsync(CredentialValidationRequest request)
    {
        try
        {
            var response = await _identityApi.ValidateCredentialsAsync(request);
            return response.IsSuccessful ? response.Content : null;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return null;
        }
    }

    public async Task<ActivateUserResponse?> ActivateUserAsync(ActivateUserRequest request)
    {
        try
        {
            var response = await _identityApi.ActivateUserAsync(request);
            return response.IsSuccessful ? response.Content : null;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
