namespace ShopIt.Identity.Presentation.Users.Responses;

public record GetUserClaimsResponse(
    Guid UserId,
    List<UserClaimResponse> Claims
);
