using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Application.Roles.Commands.CreateRole;

public record CreateRoleResult(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    PermissionMultiTenancySide MultiTenancySide);
