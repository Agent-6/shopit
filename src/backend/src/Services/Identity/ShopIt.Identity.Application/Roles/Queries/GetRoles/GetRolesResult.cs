using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoles;

public record GetRolesRoleItem(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    PermissionMultiTenancySide MultiTenancySide);

public record GetRolesResult(
    IEnumerable<GetRolesRoleItem> Roles,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
