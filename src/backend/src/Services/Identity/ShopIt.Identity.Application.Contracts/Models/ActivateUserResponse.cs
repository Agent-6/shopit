namespace ShopIt.Identity.Application.Contracts.Models;

/// <summary>
/// Outcome of an activation attempt. On success <see cref="Succeeded"/> is <c>true</c> and
/// the account details are populated so the Authentication service can sign the user in
/// (zero extra login steps). On failure <see cref="ErrorCode"/>/<see cref="Error"/> describe
/// the reason (expired/invalid token, unknown user, password policy violation).
/// </summary>
public record ActivateUserResponse(
    bool Succeeded,
    Guid UserId,
    Guid TenantId,
    string UserName,
    string Email,
    string? ErrorCode = null,
    string? Error = null);
