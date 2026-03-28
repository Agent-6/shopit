namespace ShopIt.Identity.Application.Users.Queries.GetUsers;

public record GetUsersResult(
    IEnumerable<GetUsersUserItem> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
