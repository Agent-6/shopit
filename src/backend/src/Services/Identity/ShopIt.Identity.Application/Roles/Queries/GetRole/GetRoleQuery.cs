using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Roles.Queries.GetRole;

public record GetRoleQuery(Guid RoleId) : IQuery<GetRoleResult>;
