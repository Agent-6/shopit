using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Application.Roles.Commands.CreateRole;

public record CreateRoleCommand(
    string Name,
    string? Description,
    PermissionMultiTenancySide? MultiTenancySide = null) : ICommand<CreateRoleResult>;
