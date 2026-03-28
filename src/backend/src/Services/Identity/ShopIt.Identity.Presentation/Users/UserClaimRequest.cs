namespace ShopIt.Identity.Presentation.Users;

public record UserClaimRequest(
    string ClaimType,
    string ClaimValue
);
