using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Presentation.Permissions.Responses;

public record PermissionDefinitionResponse(
    string Name,
    string DisplayName,
    string? Description,
    PermissionMultiTenancySide MultiTenancySide
);

public record PermissionGroupResponse(
    string Name,
    string DisplayName,
    List<PermissionDefinitionResponse> Permissions
);

public record GetPermissionDefinitionsResponse(
    List<PermissionGroupResponse> Groups
);

public record PermissionMatrixClaimResponse(
    string Type,
    string Value
);

public record PermissionMatrixRoleResponse(
    Guid Id,
    string Name,
    Guid TenantId,
    PermissionMultiTenancySide MultiTenancySide,
    List<PermissionMatrixClaimResponse> Claims
);

public record GetPermissionMatrixResponse(
    List<PermissionGroupResponse> Groups,
    List<PermissionMatrixRoleResponse> Roles
);
