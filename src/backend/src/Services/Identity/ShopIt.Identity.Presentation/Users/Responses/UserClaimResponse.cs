namespace ShopIt.Identity.Presentation.Users.Responses;

public record UserClaimResponse(
    string ClaimType,
    string ClaimValue
);
