using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Users.Queries.GetUserRoles;

public record GetUserRolesQuery(Guid UserId) : IQuery<Result<GetUserRolesResult, Error>>;
