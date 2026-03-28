namespace ShopIt.Identity.Presentation.Users;

public record GetUserClaimsResponse(
    Guid UserId,
    List<UserClaimResponse> Claims
);
