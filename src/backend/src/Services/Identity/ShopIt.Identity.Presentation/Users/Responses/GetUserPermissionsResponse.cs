namespace ShopIt.Identity.Presentation.Users.Responses;

public record GetUserPermissionsResponse(
    Guid UserId,
    List<UserPermissionResponse> Permissions,
    List<InheritedPermissionResponse> InheritedPermissions
);
