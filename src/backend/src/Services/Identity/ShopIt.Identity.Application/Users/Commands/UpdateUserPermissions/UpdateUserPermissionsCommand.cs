using ShopIt.Framework.Core.CQRS.Commands;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions;

public record PermissionUpdateItem(string PermissionName, bool IsGranted);

public record UpdateUserPermissionsCommand(Guid UserId, IEnumerable<PermissionUpdateItem> Permissions) : ICommand<UpdateUserPermissionsResult>;

