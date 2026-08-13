namespace ShopIt.Identity.Application.Roles.Commands.UpdateRoleClaims;

public record UpdateRoleClaimsResult(Guid RoleId, IEnumerable<RoleClaimUpdateItem> Claims, DateTime UpdatedAt);
