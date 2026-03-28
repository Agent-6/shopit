namespace ShopIt.Identity.Application.Users.Queries.GetUserClaims;

public record GetUserClaimsResult(Guid UserId, IEnumerable<UserClaimItem> Claims);

public record UserClaimItem(string Type, string Value);
