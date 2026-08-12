namespace ShopIt.Identity.Application.Contracts.Models;

public record CredentialValidationResponse(Guid UserId, Guid TenantId, string UserName, string Email, bool EmailConfirmed);
