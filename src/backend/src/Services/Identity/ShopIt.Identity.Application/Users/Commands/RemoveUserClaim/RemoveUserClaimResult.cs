namespace ShopIt.Identity.Application.Users.Commands.RemoveUserClaim;

public record RemoveUserClaimResult(Guid UserId, string ClaimType, string ClaimValue, bool Removed);
