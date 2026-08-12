namespace ShopIt.Identity.Presentation.Permissions.Responses;

public record PermissionDefinitionResponse(
    string Name,
    string DisplayName,
    string? Description
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
    List<PermissionMatrixClaimResponse> Claims
);

public record GetPermissionMatrixResponse(
    List<PermissionGroupResponse> Groups,
    List<PermissionMatrixRoleResponse> Roles
);
