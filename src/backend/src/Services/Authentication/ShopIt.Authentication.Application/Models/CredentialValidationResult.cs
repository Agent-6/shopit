namespace ShopIt.Authentication.Application.Models;

public record CredentialValidationResult(Guid UserId, Guid TenantId, string UserName, string Email);
