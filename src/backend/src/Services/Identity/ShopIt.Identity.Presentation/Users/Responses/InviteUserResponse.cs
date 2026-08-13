namespace ShopIt.Identity.Presentation.Users.Responses;

public record InviteUserResponse(
    Guid Id,
    string Email,
    string Status,
    DateTimeOffset? InvitationExpiresAt);
