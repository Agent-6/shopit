namespace ShopIt.Identity.Presentation.Users.Requests;

public record UpdateUserClaimsRequest(
    List<UserClaimRequest> Claims,
    List<string>? RemovedClaims = null
);
