using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.UnlockUser;

public record UnlockUserCommand(Guid UserId) : ICommand<UnlockUserResult>;
