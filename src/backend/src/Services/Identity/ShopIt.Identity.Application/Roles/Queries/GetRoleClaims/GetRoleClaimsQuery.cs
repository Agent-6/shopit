using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoleClaims;

public record GetRoleClaimsQuery(Guid RoleId) : IQuery<Result<GetRoleClaimsResult, Error>>;
