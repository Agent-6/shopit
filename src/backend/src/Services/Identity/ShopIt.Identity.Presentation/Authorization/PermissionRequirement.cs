using Microsoft.AspNetCore.Authorization;

namespace ShopIt.Identity.Presentation.Authorization;

/// <summary>
/// Authorization requirement that is satisfied when the current user holds the given
/// permission — either as a direct claim on the user account, or inherited from one of
/// the user's roles (permissions are stored as claims).
/// </summary>
public sealed class PermissionRequirement(string permissionName) : IAuthorizationRequirement
{
    public string PermissionName { get; } = permissionName;
}
