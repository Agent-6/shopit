using ShopIt.Identity.Presentation.Users.Requests;

namespace ShopIt.Identity.Presentation.Users.Responses;

public record UpdateUserClaimsResponse(
    Guid UserId,
    List<UserClaimRequest> UpdatedClaims,
    List<string> RemovedClaims,
    DateTime UpdatedAt
);
