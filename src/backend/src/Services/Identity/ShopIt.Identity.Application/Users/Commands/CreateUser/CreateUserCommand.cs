using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

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
    IEnumerable<CreateUserClaimItem>? Claims = null) : ICommand<Result<CreateUserResult, Error>>;
