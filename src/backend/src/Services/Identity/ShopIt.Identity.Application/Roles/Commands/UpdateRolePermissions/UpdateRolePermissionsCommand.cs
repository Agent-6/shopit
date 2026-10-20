using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRolePermissions;

public record PermissionUpdateItem(string PermissionName, bool IsGranted);

public record UpdateRolePermissionsCommand(Guid RoleId, IEnumerable<PermissionUpdateItem> Permissions)
    : ICommand<Result<UpdateRolePermissionsResult, Error>>;
