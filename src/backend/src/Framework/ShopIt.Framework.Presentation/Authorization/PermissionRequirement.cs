using Microsoft.AspNetCore.Authorization;

namespace ShopIt.Framework.Presentation.Authorization;

/// <summary>
/// Authorization requirement satisfied when the current user holds the given permission — either as a
/// direct claim on the user account, or inherited from one of the user's roles (permissions are stored
/// as claims).
/// </summary>
/// <remarks>
/// <para>
/// Only the requirement and the endpoint convention are shared. <em>How</em> the permission is resolved
/// is a service concern — Identity reads its own database, Tenancy asks Identity over HTTP — so each
/// service registers its own <see cref="IAuthorizationHandler"/>.
/// </para>
/// <para>
/// Whether those two resolution paths should converge is a separate question; see
/// <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </para>
/// </remarks>
public sealed class PermissionRequirement(string permissionName) : IAuthorizationRequirement
{
    public string PermissionName { get; } = permissionName;
}
