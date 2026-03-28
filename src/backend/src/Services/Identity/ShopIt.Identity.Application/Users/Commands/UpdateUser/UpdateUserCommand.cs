using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid UserId,
    string? Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool? IsActive
) : ICommand<UpdateUserResult>;

