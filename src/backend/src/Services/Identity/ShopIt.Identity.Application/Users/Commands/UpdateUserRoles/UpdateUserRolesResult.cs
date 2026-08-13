namespace ShopIt.Identity.Application.Users.Commands.UpdateUserRoles;

public record UpdateUserRolesResult(Guid UserId, IEnumerable<string> Roles, DateTime UpdatedAt);
