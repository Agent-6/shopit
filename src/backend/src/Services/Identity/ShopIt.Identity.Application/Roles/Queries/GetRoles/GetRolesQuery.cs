using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Roles.Queries.GetRoles;

public record GetRolesQuery(int PageNumber, int PageSize, string? Filter) : IQuery<GetRolesResult>;
