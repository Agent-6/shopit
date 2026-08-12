namespace ShopIt.Identity.Application.Roles.Queries.GetRoles;

public record GetRolesRoleItem(Guid Id, string Name, string? Description, DateTime CreatedAt);

public record GetRolesResult(
    IEnumerable<GetRolesRoleItem> Roles,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
