namespace ShopIt.Identity.Presentation.Users;

public record UserPermissionResponse(
    string PermissionName,
    bool IsGranted,
    PermissionSource Source
);
