using ShopIt.Framework.Core.CQRS.Commands;

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
) : ICommand<UpdateUserResult>;
