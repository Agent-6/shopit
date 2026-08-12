namespace ShopIt.Identity.Presentation.Roles.Responses;

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt
);

public record GetRolesResponse
{
    public required List<RoleResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}

public record RoleClaimResponse(string Type, string Value);

public record GetRoleClaimsResponse(Guid RoleId, List<RoleClaimResponse> Claims);

public record RoleDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    List<RoleClaimResponse> Claims
);

public record CreateRoleResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt
);

public record UpdateRoleResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime UpdatedAt
);

public record DeleteRoleResponse(Guid Id, bool IsDeleted);

public record UpdateRoleClaimsResponse(
    Guid RoleId,
    List<RoleClaimResponse> Claims,
    DateTime UpdatedAt
);
