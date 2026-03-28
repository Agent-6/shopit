namespace ShopIt.Identity.Presentation.Users;

public record GetUserPermissionsResponse(
    Guid UserId,
    List<UserPermissionResponse> Permissions,
    List<InheritedPermission> InheritedPermissions
);
