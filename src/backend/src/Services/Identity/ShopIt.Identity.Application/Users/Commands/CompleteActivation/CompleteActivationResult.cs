namespace ShopIt.Identity.Application.Users.Commands.CompleteActivation;

/// <summary>
/// Outcome of an activation attempt. On success the account details are populated so the
/// Authentication service can sign the user in immediately. On failure
/// <see cref="ErrorCode"/> is one of <c>USER_NOT_FOUND</c>, <c>ACTIVATION_TOKEN_EXPIRED</c>,
/// <c>ACTIVATION_TOKEN_INVALID</c>, <c>PASSWORD_POLICY</c> or <c>ACTIVATION_FAILED</c>.
/// </summary>
public record CompleteActivationResult(
    bool Succeeded,
    Guid UserId,
    Guid TenantId,
    string UserName,
    string Email,
    string? ErrorCode = null,
    string? Error = null);
