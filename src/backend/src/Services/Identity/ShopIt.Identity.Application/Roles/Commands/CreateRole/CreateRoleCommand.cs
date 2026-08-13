using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Name, string? Description) : ICommand<CreateRoleResult>;
