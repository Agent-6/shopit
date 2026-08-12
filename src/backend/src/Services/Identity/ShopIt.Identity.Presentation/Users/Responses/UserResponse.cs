namespace ShopIt.Identity.Presentation.Users.Responses;

// Response Records
public record UserResponse(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    bool EmailConfirmed,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    DateTime CreatedAt,
    DateTime LastModifiedAt,
    List<string> Roles
);
