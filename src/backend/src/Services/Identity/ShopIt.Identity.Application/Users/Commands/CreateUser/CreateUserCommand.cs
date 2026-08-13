using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.CreateUser;

public record CreateUserClaimItem(string Type, string Value);

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    IEnumerable<string>? Roles = null,
    IEnumerable<CreateUserClaimItem>? Claims = null) : ICommand<CreateUserResult>;
