namespace ShopIt.Identity.Application.Users.Queries.GetUser;

public record GetUserResult(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive
);
