namespace ShopIt.Identity.Client.Models;

/// <summary>
/// Response of Identity's internal <c>/api/internal/users/{id}/permissions</c> endpoint: the user's
/// effective permissions, already resolved and filtered by multi-tenancy side.
/// </summary>
public record UserPermissionsResponse(IReadOnlyCollection<string> Permissions);
