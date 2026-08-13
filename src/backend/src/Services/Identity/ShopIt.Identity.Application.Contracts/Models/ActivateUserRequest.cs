namespace ShopIt.Identity.Application.Contracts.Models;

/// <summary>
/// Payload for the synchronous activation call made by the Authentication service when
/// the invited user submits their new password on the activation page.
/// </summary>
public record ActivateUserRequest(
    Guid UserId,
    string Token,
    string Password);
