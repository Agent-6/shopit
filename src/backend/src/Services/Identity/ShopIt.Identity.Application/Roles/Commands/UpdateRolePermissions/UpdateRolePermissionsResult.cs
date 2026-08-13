namespace ShopIt.Identity.Application.Roles.Commands.UpdateRolePermissions;

public record UpdateRolePermissionsResult(Guid RoleId, IEnumerable<string> GrantedPermissions, IEnumerable<string> RevokedPermissions, DateTime UpdatedAt);
