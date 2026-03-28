namespace ShopIt.Identity.Application.Users.Commands.UpdateUserClaims;

public record UpdateUserClaimsResult(Guid UserId, IEnumerable<UserClaimUpdateItem> UpdatedClaims, IEnumerable<UserClaimUpdateItem> RemovedClaims, DateTime UpdatedAt);
