using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Presentation.Roles.Requests;

public record CreateRoleRequest(
    string Name,
    string? Description = null,
    PermissionMultiTenancySide? MultiTenancySide = null
);

public record UpdateRoleRequest(
    string Name,
    string? Description = null
);

public record RolePermissionRequest(
    string PermissionName,
    bool IsGranted
);

public record UpdateRolePermissionsRequest(
    List<RolePermissionRequest> Permissions
);
