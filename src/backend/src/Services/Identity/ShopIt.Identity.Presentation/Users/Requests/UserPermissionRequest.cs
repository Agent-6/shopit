namespace ShopIt.Identity.Presentation.Users.Requests;

public record UserPermissionRequest(
    string PermissionName,
    bool IsGranted
);
