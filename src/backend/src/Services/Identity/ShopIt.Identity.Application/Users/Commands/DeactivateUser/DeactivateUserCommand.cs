using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid UserId, string? Reason) : ICommand<DeactivateUserResult>;
