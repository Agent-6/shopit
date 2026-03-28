namespace ShopIt.Identity.Presentation.Users;

public record UserClaimResponse(
    string ClaimType,
    string ClaimValue
);
