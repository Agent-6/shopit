namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions;

public record UpdateUserPermissionsResult(Guid UserId, IEnumerable<string> GrantedPermissions, IEnumerable<string> RevokedPermissions, DateTime UpdatedAt);
