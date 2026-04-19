using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ShopIt.Authentication.Application.Models;
using ShopIt.Authentication.Application.Services;

namespace ShopIt.Authentication.Infrastructure.Services;

public class IdentityServiceClient : IIdentityServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public IdentityServiceClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<CredentialValidationResult?> ValidateCredentialsAsync(CredentialValidationRequest request)
    {
        // Add mocked user logic for now
        if (request.Username == "mock@user.com" && request.Password == "mockpassword")
        {
            // WATCH: empty guid for host user.
            return new CredentialValidationResult(Guid.NewGuid(), Guid.Empty, "Mock User", "mock@user.com");
        }

        if (request.Username == "tenant@user.com" && request.Password == "mockpassword")
        {
            return new CredentialValidationResult(Guid.NewGuid(), new Guid("B5D0C0E4-3A5B-4CDC-8D2A-7F1F6C9F5B4E"), "Tenant User", "tenant@user.com");
        }

        var payload = new
        {
            request.Username,
            request.Password,
        };

        // TODO: remove this after implementing client credintails auth
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config["ApiKeys:IdentityService"]);

        var response = await _httpClient.PostAsJsonAsync("api/internal/validate-credentials", payload);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CredentialValidationResult>();
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        // Mock successful email sending
        await Task.Delay(500);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        // Mock successful password reset
        await Task.Delay(500);
        return true;
    }
}
