namespace ShopIt.Identity.Application.Users.Commands.CreateUser;

public record CreateUserResult(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber);
