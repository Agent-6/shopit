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
            var response = await _identityApi.ValidateCredentialsAsync(request);
            return response.IsSuccessful ? response.Content : null;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                       or System.Net.HttpStatusCode.Forbidden)
        {
            return null;
        }
    }
}
