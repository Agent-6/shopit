using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.ActivateUser;

public record ActivateUserCommand(Guid UserId) : ICommand<ActivateUserResult>;
