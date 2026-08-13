namespace ShopIt.Identity.Presentation.Users.Requests;

/// <summary>
/// Payload for POST /users/invite — provisions a user in PendingActivation state and
/// triggers the invitation email. No password: the invited user chooses it via the
/// activation link.
/// </summary>
public record InviteUserRequest(
    string Email,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    List<string>? Roles = null,
    List<UserClaimRequest>? Claims = null);
