using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Users.Queries.GetUserPermissions;

public record GetUserPermissionsQuery(Guid UserId) : IQuery<Result<GetUserPermissionsResult, Error>>;

