namespace ShopIt.Identity.Presentation.Users;

public record UpdateUserRequest(
    string? Username = null,
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    bool? IsActive = null,
    List<string>? Roles = null,
    List<UserClaimRequest>? Claims = null
);
