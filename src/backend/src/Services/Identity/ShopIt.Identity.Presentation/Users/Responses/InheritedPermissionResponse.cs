namespace ShopIt.Identity.Presentation.Users.Responses;

public record InheritedPermissionResponse(
    string PermissionName,
    string Source
);
