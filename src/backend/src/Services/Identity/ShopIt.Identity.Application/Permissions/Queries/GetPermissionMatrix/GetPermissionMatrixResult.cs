namespace ShopIt.Identity.Application.Permissions.Queries.GetPermissionMatrix;

public record GetPermissionMatrixResult(
    IReadOnlyCollection<PermissionMatrixGroupItem> Groups,
    IReadOnlyCollection<PermissionMatrixRoleItem> Roles);

public record PermissionMatrixGroupItem(
    string Name,
    string DisplayName,
    IReadOnlyCollection<PermissionMatrixDefinitionItem> Permissions);

public record PermissionMatrixDefinitionItem(string Name, string DisplayName, string? Description);

public record PermissionMatrixRoleItem(Guid Id, string Name, IReadOnlyCollection<PermissionMatrixClaimItem> Claims);

public record PermissionMatrixClaimItem(string Type, string Value);
