using ShopIt.Framework.Domain.Errors;

namespace ShopIt.Tenancy.Domain.Tenants;

/// <summary>
/// The expected failures of tenant operations, as values.
/// </summary>
/// <remarks>
/// "Not found" and "name already taken" are outcomes a caller can act on — they belong in a
/// <c>Result</c>, not in an exception that has to be caught and mapped back to a status code (#32).
/// Argument violations inside <see cref="Entities.Tenant"/> stay exceptions: those are bugs in the
/// caller, not something a client can recover from.
/// </remarks>
public static class TenantErrors
{
    /// <summary>The tenant does not exist.</summary>
    public static Error NotFound(Guid tenantId) =>
        new("Tenant.NotFound", $"A tenant with the id '{tenantId}' was not found.", ErrorType.NotFound);

    /// <summary>Another tenant already uses this name.</summary>
    public static Error NameAlreadyExists(string name) =>
        new("Tenant.NameAlreadyExists", $"A tenant named '{name}' already exists.", ErrorType.Conflict);
}
