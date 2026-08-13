namespace ShopIt.Identity.Application.Contracts.Models;

/// <summary>
/// Result of a credential validation attempt during login.
/// </summary>
/// <param name="Success"><c>true</c> when the credentials are valid and the account may sign in.</param>
/// <param name="ErrorCode">Machine-readable failure code, e.g. <c>ACCOUNT_NOT_ACTIVATED</c> or <c>ACCOUNT_DISABLED</c>.</param>
/// <param name="Message">Human-readable failure description.</param>
/// <param name="UserId">Populated on success.</param>
/// <param name="TenantId">Populated on success.</param>
/// <param name="UserName">Populated on success.</param>
/// <param name="Email">Populated on success.</param>
/// <param name="EmailConfirmed">Populated on success.</param>
public record CredentialValidationResponse(
    bool Success,
    string? ErrorCode = null,
    string? Message = null,
    Guid UserId = default,
    Guid TenantId = default,
    string? UserName = null,
    string? Email = null,
    bool EmailConfirmed = false);
