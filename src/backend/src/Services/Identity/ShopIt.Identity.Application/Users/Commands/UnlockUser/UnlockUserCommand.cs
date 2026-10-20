using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UnlockUser;

public record UnlockUserCommand(Guid UserId) : ICommand<Result<UnlockUserResult, Error>>;
