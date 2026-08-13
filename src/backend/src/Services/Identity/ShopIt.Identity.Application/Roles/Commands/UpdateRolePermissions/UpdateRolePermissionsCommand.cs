using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRolePermissions;

public record PermissionUpdateItem(string PermissionName, bool IsGranted);

public record UpdateRolePermissionsCommand(Guid RoleId, IEnumerable<PermissionUpdateItem> Permissions) : ICommand<UpdateRolePermissionsResult>;
