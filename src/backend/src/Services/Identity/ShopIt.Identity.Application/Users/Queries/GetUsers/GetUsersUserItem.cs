namespace ShopIt.Identity.Application.Users.Queries.GetUsers;

public record GetUsersUserItem(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive
);
