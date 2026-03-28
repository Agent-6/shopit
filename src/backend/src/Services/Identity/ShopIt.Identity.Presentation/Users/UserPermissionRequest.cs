namespace ShopIt.Identity.Presentation.Users;

public record UserPermissionRequest(
    string PermissionName,
    bool IsGranted
);
