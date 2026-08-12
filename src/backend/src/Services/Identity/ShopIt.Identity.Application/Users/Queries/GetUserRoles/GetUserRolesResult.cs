namespace ShopIt.Identity.Application.Users.Queries.GetUserRoles;

public record GetUserRolesResult(Guid UserId, IEnumerable<string> Roles);
