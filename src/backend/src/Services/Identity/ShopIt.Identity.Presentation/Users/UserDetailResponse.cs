namespace ShopIt.Identity.Presentation.Users;

public record UserDetailResponse(
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
    DateTime CreatedAt,
    DateTime LastModifiedAt,
    List<string> Roles,
    List<UserClaimResponse> Claims
);
