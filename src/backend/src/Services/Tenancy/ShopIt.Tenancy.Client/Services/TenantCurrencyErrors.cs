using ShopIt.Framework.Domain.Errors;

namespace ShopIt.Tenancy.Client.Services;

/// <summary>
/// The expected failures of a tenant-currency lookup, as values.
/// </summary>
/// <remarks>
/// They live in the client rather than in each consumer because both sides of the edge must agree on them: the
/// catalog maps them to a status code, and a second consumer would otherwise invent a second wording for the same
/// outcome.
/// </remarks>
public static class TenantCurrencyErrors
{
    /// <summary>The tenant registry does not know the tenant.</summary>
    /// <remarks>
    /// <see cref="ErrorType.NotFound"/> — the registry answered, and its answer is "no such tenant". Reporting it as
    /// a 404 on the write that needed the currency is honest: the caller's token names a tenant that was never
    /// registered (or was deleted), and the request cannot be completed until it is.
    /// </remarks>
    /// <param name="tenantId">The tenant that was asked about.</param>
    public static Error TenantNotFound(Guid tenantId) =>
        new(
            "TenantCurrency.TenantNotFound",
            $"A tenant with the id '{tenantId}' was not found in the tenant registry.",
            ErrorType.NotFound);

    /// <summary>The tenant registry could not be asked.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="ErrorType.Unexpected"/> — a 500 — because the caller did nothing wrong. Deliberately <em>not</em> a
    /// fallback to a platform-wide currency: silently stamping a price with a guessed unit is a financial error
    /// whose only symptom appears later, in a shop that charged the wrong money. A refused write is visible
    /// immediately and cannot be mistaken for success.
    /// </para>
    /// <para>
    /// The service is named, not the underlying exception: the transport detail is logged where it happens, and a
    /// caller must not depend on it.
    /// </para>
    /// </remarks>
    /// <param name="tenantId">The tenant that could not be resolved.</param>
    public static Error Unavailable(Guid tenantId) =>
        new(
            "TenantCurrency.Unavailable",
            $"The tenant registry could not be reached to resolve the currency of tenant '{tenantId}'. " +
            "The request was refused rather than priced in a guessed currency; retry once the registry is available.",
            ErrorType.Unexpected);

    /// <summary>The registry answered with something that is not a currency code.</summary>
    /// <remarks>
    /// Separate from <see cref="Unavailable"/> because it is a different bug with a different fix: the call worked
    /// and the payload was wrong, so retrying will not help.
    /// </remarks>
    /// <param name="tenantId">The tenant that was asked about.</param>
    /// <param name="currency">The value the registry returned, for the message.</param>
    public static Error InvalidResponse(Guid tenantId, string? currency) =>
        new(
            "TenantCurrency.InvalidResponse",
            $"The tenant registry returned '{currency}' as the currency of tenant '{tenantId}', " +
            "which is not an ISO 4217 code.",
            ErrorType.Unexpected);
}
