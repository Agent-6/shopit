using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId, bool Permanent) : ICommand<DeleteUserResult>;

