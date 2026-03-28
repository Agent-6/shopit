namespace ShopIt.Identity.Presentation.Users;

public record UpdateUserClaimsRequest(
    List<UserClaimRequest> Claims,
    List<string>? RemovedClaims = null
);
