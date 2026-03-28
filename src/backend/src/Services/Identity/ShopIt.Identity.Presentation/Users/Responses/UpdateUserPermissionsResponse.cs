namespace ShopIt.Identity.Presentation.Users.Responses;

public record UpdateUserPermissionsResponse(
    Guid UserId,
    IEnumerable<string> GrantedPermissions,
    IEnumerable<string> RevokedPermissions,
    DateTime UpdatedAt
);
