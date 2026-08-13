using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Users.Queries.GetUserRoles;

public record GetUserRolesQuery(Guid UserId) : IQuery<GetUserRolesResult>;
