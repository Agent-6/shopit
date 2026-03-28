namespace ShopIt.Identity.Presentation.Users.Responses;

public record CreateUserResponse(
    Guid Id,
    string Username,
    string Email,
    DateTime CreatedAt
);
