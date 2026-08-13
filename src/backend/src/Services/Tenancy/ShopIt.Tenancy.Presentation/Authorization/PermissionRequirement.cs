using Microsoft.AspNetCore.Authorization;

namespace ShopIt.Tenancy.Presentation.Authorization;

/// <summary>
/// Authorization requirement satisfied when the calling user holds the given permission
/// (resolved from the Identity service via <see cref="Application.Permissions.ITenantPermissionClient"/>).
/// </summary>
public sealed class PermissionRequirement(string permissionName) : IAuthorizationRequirement
{
    public string PermissionName { get; } = permissionName;
}
