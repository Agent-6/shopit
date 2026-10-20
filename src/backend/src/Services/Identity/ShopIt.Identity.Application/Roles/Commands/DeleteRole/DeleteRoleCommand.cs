using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Roles.Commands.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : ICommand<Result<DeleteRoleResult, Error>>;
