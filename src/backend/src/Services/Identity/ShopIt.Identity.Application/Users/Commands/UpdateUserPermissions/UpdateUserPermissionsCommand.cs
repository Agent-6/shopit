using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserPermissions;

public record PermissionUpdateItem(string PermissionName, bool IsGranted);

public record UpdateUserPermissionsCommand(Guid UserId, IEnumerable<PermissionUpdateItem> Permissions) : ICommand<Result<UpdateUserPermissionsResult, Error>>;

