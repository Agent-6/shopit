namespace ShopIt.Identity.Presentation.Users;

public record UpdateUserPermissionsResponse(
    Guid UserId,
    IEnumerable<string> GrantedPermissions,
    IEnumerable<string> RevokedPermissions,
    DateTime UpdatedAt
);
