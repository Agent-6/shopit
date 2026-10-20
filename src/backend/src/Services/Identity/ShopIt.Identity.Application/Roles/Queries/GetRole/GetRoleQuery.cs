using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Roles.Queries.GetRole;

public record GetRoleQuery(Guid RoleId) : IQuery<Result<GetRoleResult, Error>>;
