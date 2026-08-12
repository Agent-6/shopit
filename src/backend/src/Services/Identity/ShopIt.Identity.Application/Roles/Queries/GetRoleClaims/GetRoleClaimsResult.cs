namespace ShopIt.Identity.Application.Roles.Queries.GetRoleClaims;

public record RoleClaimItem(string Type, string Value);

public record GetRoleClaimsResult(Guid RoleId, IEnumerable<RoleClaimItem> Claims);
