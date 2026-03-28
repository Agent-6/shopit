namespace ShopIt.Identity.Presentation.Users.Requests;

// Request Records
public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    List<string>? Roles = null,
    List<UserClaimRequest>? Claims = null,
    bool? EmailConfirmed = false,
    bool? PhoneNumberConfirmed = false,
    bool? IsActive = true
);
