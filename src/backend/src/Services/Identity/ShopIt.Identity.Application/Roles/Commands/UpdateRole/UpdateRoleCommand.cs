using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : ICommand<Result<UpdateRoleResult, Error>>;
