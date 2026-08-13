namespace ShopIt.Identity.Application.Users.Activation;

/// <summary>
/// Issues and validates time-limited, cryptographically signed activation tokens
/// used by the invite flow.
/// </summary>
public interface IActivationTokenProvider
{
    /// <summary>
    /// Issues a new activation token for the given user (valid for 48 hours).
    /// </summary>
    ActivationToken Issue(Guid userId);

    /// <summary>
    /// Validates that the token was issued for <paramref name="userId"/> and has not expired.
    /// </summary>
    ActivationTokenValidationResult Validate(Guid userId, string token);
}
