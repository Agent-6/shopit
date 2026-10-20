using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUser;

public record UpdateUserClaimItem(string Type, string Value);

public record UpdateUserCommand(
    Guid UserId,
    string? Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool? IsActive,
    IEnumerable<string>? Roles = null,
    IEnumerable<UpdateUserClaimItem>? Claims = null,
    bool? EmailConfirmed = null
) : ICommand<Result<UpdateUserResult, Error>>;
