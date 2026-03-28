namespace ShopIt.Identity.Presentation.Users.Requests;

public record UserClaimRequest(
    string ClaimType,
    string ClaimValue
);
