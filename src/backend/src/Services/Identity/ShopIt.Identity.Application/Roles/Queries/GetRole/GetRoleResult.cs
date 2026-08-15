using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Application.Roles.Queries.GetRole;

public record GetRoleResult(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    PermissionMultiTenancySide MultiTenancySide);
