namespace ShopIt.Identity.Application.Users.Commands.InviteUser;

public record InviteUserResult(Guid Id, string Email, string Status, DateTimeOffset? InvitationExpiresAt);
