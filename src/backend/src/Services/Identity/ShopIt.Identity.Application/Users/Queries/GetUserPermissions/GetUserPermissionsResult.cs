namespace ShopIt.Identity.Application.Users.Queries.GetUserPermissions;

public record GetUserPermissionsResult(Guid UserId, IEnumerable<UserPermissionItem> Permissions, IEnumerable<InheritedPermissionItem> InheritedPermissions);

public record UserPermissionItem(string PermissionName, bool IsGranted, string Source);
public record InheritedPermissionItem(string Permission, string Source);
