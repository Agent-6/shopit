using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : ICommand<UpdateRoleResult>;
