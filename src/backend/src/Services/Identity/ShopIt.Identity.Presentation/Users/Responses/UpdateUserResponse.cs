namespace ShopIt.Identity.Presentation.Users.Responses;

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
