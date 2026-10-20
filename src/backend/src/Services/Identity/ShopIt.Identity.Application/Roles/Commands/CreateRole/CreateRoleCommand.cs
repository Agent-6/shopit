using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Roles.Commands.CreateRole;

public record CreateRoleCommand(
    string Name,
    string? Description,
    PermissionMultiTenancySide? MultiTenancySide = null) : ICommand<Result<CreateRoleResult, Error>>;
