namespace ShopIt.Identity.Presentation.Users.Requests;

public record UpdateUserPermissionsRequest(
    List<UserPermissionRequest> Permissions
);
