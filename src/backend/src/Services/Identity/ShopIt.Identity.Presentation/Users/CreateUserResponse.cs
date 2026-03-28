namespace ShopIt.Identity.Presentation.Users;

public record CreateUserResponse(
    Guid Id,
    string Username,
    string Email,
    DateTime CreatedAt
);
