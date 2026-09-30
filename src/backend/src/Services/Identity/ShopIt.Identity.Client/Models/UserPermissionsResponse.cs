namespace ShopIt.Identity.Client.Models;

/// <summary>
/// Response of Identity's internal <c>/api/internal/users/{id}/permissions</c> endpoint: the user's
/// effective permissions, already resolved and filtered by multi-tenancy side.
/// </summary>
/// <param name="IsAllPermissions">
/// Whether the user holds a role whose definition grants every permission. When true,
/// <paramref name="Permissions"/> is empty — holding everything is represented by the flag, not by
/// enumerating the catalog. Testing the flag rather than the list is what keeps a published catalog from
/// having to invalidate a cache entry per user.
/// </param>
/// <param name="Permissions">The permissions granted explicitly.</param>
public record UserPermissionsResponse(bool IsAllPermissions, IReadOnlyCollection<string> Permissions);
