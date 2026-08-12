namespace ShopIt.Identity.Application.Users.Queries.GetUsers;

public record GetUsersUserItem(
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
    IEnumerable<string> Roles
);
