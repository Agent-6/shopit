using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid UserId, string? Reason) : ICommand<Result<DeactivateUserResult, Error>>;
