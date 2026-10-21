using System.Globalization;
using System.Text.Json.Serialization;
using ShopIt.Framework.Domain.Errors;

namespace ShopIt.Catalog.Domain.Products;

/// <summary>
/// An amount of money in one currency, as the catalog stores it.
/// </summary>
/// <remarks>
/// <para>
/// This is ADR-0005's value object: a <see cref="decimal"/> with at most two decimal places, never
/// negative, paired with an ISO 4217 currency code. Storage does not round — a price with three decimals
/// is rejected by <see cref="Validate"/>, because silently turning <c>19.999</c> into <c>20.00</c> changes
/// what a shop charges.
/// </para>
/// <para>
/// The ADR's other half — that the currency is the <em>tenant's</em>, not the product's — is not enforced
/// here because the platform has no per-tenant currency yet (#84). Until it does, the catalog stamps the
/// configured <c>Catalog:Currency</c> on every price and rejects a request that names a different one, and
/// <see cref="CurrencyCode"/> is stored with the price so a row stays interpretable if that setting ever
/// changes.
/// </para>
/// <para>
/// Over HTTP it is <c>{ "amount": "19.99", "currency": "EUR" }</c> — the amount is a <em>string</em> so a
/// JavaScript client cannot round it through an IEEE-754 double. The converter is attached to the type
/// rather than configured per service, so the contract cannot differ between endpoints.
/// </para>
/// </remarks>
[JsonConverter(typeof(MoneyJsonConverter))]
public readonly record struct Money
{
    /// <summary>How many letters an ISO 4217 currency code has.</summary>
    public const int CurrencyCodeLength = 3;

    /// <summary>The number of decimal places a stored amount may have.</summary>
    public const int Scale = 2;

    /// <summary>The amount, non-negative, with at most <see cref="Scale"/> decimal places.</summary>
    public decimal Amount { get; }

    /// <summary>The ISO 4217 currency code, uppercase (for example <c>EUR</c>).</summary>
    public string CurrencyCode { get; }

    private Money(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
    }

    /// <summary>
    /// Creates a validated amount of money.
    /// </summary>
    /// <param name="amount">The amount; non-negative, at most two decimal places.</param>
    /// <param name="currencyCode">The ISO 4217 code, any casing; stored uppercase.</param>
    /// <exception cref="ArgumentException">
    /// The amount or code violates <see cref="Validate"/>. Callers that can recover — a command handler
    /// mapping to a <c>Result</c> — validate first and never reach this.
    /// </exception>
    public static Money Create(decimal amount, string? currencyCode)
    {
        if (Validate(amount, currencyCode) is { } error)
        {
            throw new ArgumentException(error.Description, nameof(amount));
        }

        return new Money(amount, NormalizeCurrencyCode(currencyCode!));
    }

    /// <summary>
    /// Checks a candidate amount and currency against this value object's invariants, returning the
    /// failure instead of throwing it.
    /// </summary>
    /// <remarks>
    /// The single definition of "a valid price": <see cref="Create"/> throws what this returns and the
    /// command handlers return it as a <c>Result</c>. The checks are ordered so the most specific message
    /// wins — a negative amount is reported as negative even if it also has too many decimals.
    /// </remarks>
    /// <param name="amount">The candidate amount.</param>
    /// <param name="currencyCode">The candidate currency code.</param>
    /// <returns>An <see cref="Error"/> when the pair is invalid, otherwise <c>null</c>.</returns>
    public static Error? Validate(decimal amount, string? currencyCode)
    {
        if (amount < 0m)
        {
            return ProductErrors.NegativePrice();
        }

        // Compare rather than round: rounding here would silently accept 19.999 and store 20.00.
        if (decimal.Round(amount, Scale) != amount)
        {
            return ProductErrors.InvalidPriceScale(Scale);
        }

        return IsValidCurrencyCode(currencyCode) ? null : ProductErrors.InvalidCurrency(currencyCode);
    }

    /// <summary>
    /// Whether <paramref name="currencyCode"/> looks like an ISO 4217 code: three ASCII letters.
    /// </summary>
    /// <remarks>
    /// Casing is accepted in any form and normalized, because "eur" and "EUR" are the same currency and
    /// rejecting one of them would be a client-experience bug rather than a data-integrity rule. The
    /// stored value is always uppercase.
    /// </remarks>
    public static bool IsValidCurrencyCode(string? currencyCode) =>
        currencyCode is { Length: CurrencyCodeLength }
        && currencyCode.All(character => char.IsAsciiLetter(character));

    /// <summary>
    /// The stored form of a currency code: trimmed and uppercased with the invariant culture.
    /// </summary>
    /// <remarks>
    /// Public because the command handlers compare a request's currency with the configured one, and two
    /// definitions of "the same currency code" is how a mismatch slips through as a match.
    /// </remarks>
    public static string NormalizeCurrencyCode(string currencyCode) =>
        currencyCode.Trim().ToUpperInvariant();

    /// <summary>
    /// The amount with exactly two decimal places, then the currency code — for logs and test failures,
    /// never for display (the portal formats with <c>Intl.NumberFormat</c>, per ADR-0005).
    /// </summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {CurrencyCode}");
}
