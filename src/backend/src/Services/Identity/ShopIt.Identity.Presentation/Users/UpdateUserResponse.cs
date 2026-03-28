namespace ShopIt.Identity.Presentation.Users;

public record UpdateUserResponse(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool IsActive,
    DateTime LastModifiedAt
);
