using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Identity.Domain.Users;

namespace ShopIt.Identity.Application.Users.Commands.UpdateUserRoles;

public record UpdateUserRolesCommand(Guid UserId, IEnumerable<string> RoleNames) : ICommand<Result<UpdateUserRolesResult, Error>>;
