namespace ShopIt.Identity.Presentation.Users;

public record UpdateUserClaimsResponse(
    Guid UserId,
    List<UserClaimRequest> UpdatedClaims,
    List<string> RemovedClaims,
    DateTime UpdatedAt
);
