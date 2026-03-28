using ShopIt.Identity.Presentation.Users.Enums;

namespace ShopIt.Identity.Presentation.Users.Responses;

public record UserPermissionResponse(
    string PermissionName,
    bool IsGranted,
    PermissionSource Source
);
