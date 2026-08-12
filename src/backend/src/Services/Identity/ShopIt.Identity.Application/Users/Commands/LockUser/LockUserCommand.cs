using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.LockUser;

public record LockUserCommand(Guid UserId, DateTimeOffset? LockoutEnd) : ICommand<LockUserResult>;
