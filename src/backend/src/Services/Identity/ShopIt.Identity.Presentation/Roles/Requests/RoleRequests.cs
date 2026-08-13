namespace ShopIt.Identity.Presentation.Roles.Requests;

public record CreateRoleRequest(
    string Name,
    string? Description = null
);

public record UpdateRoleRequest(
    string Name,
    string? Description = null
);

public record RoleClaimRequest(string ClaimType, string ClaimValue);

public record UpdateRoleClaimsRequest(
    List<RoleClaimRequest> Claims
);
