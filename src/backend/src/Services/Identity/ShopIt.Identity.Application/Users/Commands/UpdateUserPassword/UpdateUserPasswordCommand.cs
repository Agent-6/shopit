using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPassword;

public record UpdateUserPasswordCommand(Guid UserId, string NewPassword) : ICommand<Result<UpdateUserPasswordResult, Error>>;
