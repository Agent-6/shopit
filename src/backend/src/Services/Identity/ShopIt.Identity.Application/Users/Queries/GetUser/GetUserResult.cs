namespace ShopIt.Identity.Application.Users.Queries.GetUser;

public record GetUserResult(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    bool EmailConfirmed,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool TwoFactorEnabled,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    DateTime CreatedAt,
    DateTime LastModifiedAt
);
