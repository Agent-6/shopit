using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoleClaims;

public record GetRoleClaimsQuery(Guid RoleId) : IQuery<GetRoleClaimsResult>;
