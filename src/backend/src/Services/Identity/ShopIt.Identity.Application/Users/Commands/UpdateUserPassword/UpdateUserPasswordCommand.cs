using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPassword;

public record UpdateUserPasswordCommand(Guid UserId, string NewPassword) : ICommand<UpdateUserPasswordResult>;
