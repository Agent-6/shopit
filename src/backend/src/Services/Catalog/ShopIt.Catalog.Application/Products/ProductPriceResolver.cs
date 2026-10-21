using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Errors;

namespace ShopIt.Catalog.Application.Products;

/// <summary>
/// Turns a request's amount and currency into a validated <see cref="Money"/>, stamped with the currency
/// this catalog prices in.
/// </summary>
/// <remarks>
/// <para>
/// Shared by every product command so the price rules have one implementation: without it, create and update
/// drift and a price that one accepts the other refuses.
/// </para>
/// <para>
/// The currency is the <em>interim</em> platform-wide setting (#84), not the tenant's, and it is applied here
/// rather than in the aggregate because "which currency does this catalog use" is an application decision;
/// <see cref="Money"/> only knows what a valid amount of money is.
/// </para>
/// </remarks>
internal static class ProductPriceResolver
{
    /// <summary>
    /// Validates a requested price and stamps <paramref name="expectedCurrency"/> on it.
    /// </summary>
    /// <param name="amount">The requested amount, already read as a decimal.</param>
    /// <param name="currencyCode">The requested currency, or <c>null</c>/empty to accept the catalog's own.</param>
    /// <param name="expectedCurrency">The currency this catalog prices in, normalized.</param>
    /// <param name="price">The validated price, when this returns <c>null</c>.</param>
    /// <returns>An <see cref="Error"/> when the price is invalid or names another currency, otherwise <c>null</c>.</returns>
    public static Error? Resolve(
        decimal amount,
        string? currencyCode,
        string expectedCurrency,
        out Money price)
    {
        price = default;

        // Omitting the currency is allowed and means "the catalog's": a client should not have to know a
        // setting it cannot read, and the alternative — a required field whose only correct value is
        // unstated — is worse.
        var requested = string.IsNullOrWhiteSpace(currencyCode) ? expectedCurrency : currencyCode;

        // Shape first: a mismatched-but-invalid code ("Kittens") is better reported as invalid than as a
        // mismatch, because that is what the caller has to fix.
        if (Money.Validate(amount, requested) is { } error)
        {
            return error;
        }

        var normalized = Money.NormalizeCurrencyCode(requested);
        if (!string.Equals(normalized, expectedCurrency, StringComparison.Ordinal))
        {
            return ProductErrors.CurrencyMismatch(expectedCurrency, normalized);
        }

        price = Money.Create(amount, normalized);

        return null;
    }
}
