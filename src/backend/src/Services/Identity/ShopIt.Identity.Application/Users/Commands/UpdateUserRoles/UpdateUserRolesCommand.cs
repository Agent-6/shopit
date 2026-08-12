using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserRoles;

public record UpdateUserRolesCommand(Guid UserId, IEnumerable<string> RoleNames) : ICommand<UpdateUserRolesResult>;
