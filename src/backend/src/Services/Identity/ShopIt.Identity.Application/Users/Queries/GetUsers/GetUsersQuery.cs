using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Users.Queries.GetUsers;

public record GetUsersQuery(int PageNumber, int PageSize, string? Filter, string? SortBy, string? SortOrder) : IQuery<GetUsersResult>;
