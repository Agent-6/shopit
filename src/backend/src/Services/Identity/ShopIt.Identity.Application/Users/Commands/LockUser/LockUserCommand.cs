using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.LockUser;

public record LockUserCommand(Guid UserId, DateTimeOffset? LockoutEnd) : ICommand<Result<LockUserResult, Error>>;
