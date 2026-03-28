namespace ShopIt.Identity.Presentation.Users;

public record UpdateUserPermissionsRequest(
    List<UserPermissionRequest> Permissions
);
