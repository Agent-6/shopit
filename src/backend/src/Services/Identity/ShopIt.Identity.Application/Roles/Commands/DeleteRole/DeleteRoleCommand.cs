using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Roles.Commands.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : ICommand<DeleteRoleResult>;
