using ShopIt.Catalog.Domain.Products;

namespace ShopIt.Catalog.Application;

/// <summary>
/// The catalog service's own settings.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Currency"/> is the <em>interim</em> answer to "which currency does a price use". ADR-0005
/// settles that the currency belongs to the tenant, but nothing yet exposes a tenant's currency to this
/// service — the catalog has its own database, the access token carries no currency claim, and there is no
/// Tenancy client. Until #84 lands, one process-wide setting stands in for it: every price is stamped with
/// it and a request naming a different one is refused.
/// </para>
/// <para>
/// The setting is read from configuration (<c>Catalog:Currency</c>) rather than hard-coded precisely so the
/// stand-in is visible where it is configured, and so #84 replaces one source instead of hunting for a
/// constant.
/// </para>
/// </remarks>
public sealed class CatalogOptions
{
    /// <summary>The configuration section this type binds to.</summary>
    public const string SectionName = "Catalog";

    /// <summary>
    /// The ISO 4217 currency every product price in this catalog is priced in. Defaults to <c>EUR</c>, the
    /// seeded tenant's currency.
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// <see cref="Currency"/> in the stored form: trimmed and uppercased.
    /// </summary>
    /// <remarks>
    /// One definition of the normalized form, used by the command handlers when they stamp a price and when
    /// they compare a request's currency with this one — so "the setting says EUR" and "the request says eur"
    /// cannot be read as a mismatch.
    /// </remarks>
    public string NormalizedCurrency => Money.NormalizeCurrencyCode(Currency);
}
