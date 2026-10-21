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

    /// <summary>
    /// The currency is not a three-letter ISO 4217 code.
    /// </summary>
    /// <remarks>
    /// <see cref="ErrorType.Validation"/> — a 400 — because the caller can fix it: a tenant's currency decides the
    /// currency of every price it writes, so accepting a code the catalog cannot stamp would move the failure to
    /// an unrelated request later on.
    /// </remarks>
    /// <param name="currency">The code the request supplied.</param>
    public static Error InvalidCurrency(string? currency) =>
        new(
            "Tenant.InvalidCurrency",
            currency is null or ""
                ? "A tenant currency is required, as a three-letter ISO 4217 code such as 'EUR'."
                : $"'{currency}' is not an ISO 4217 currency code.",
            ErrorType.Validation);
}
