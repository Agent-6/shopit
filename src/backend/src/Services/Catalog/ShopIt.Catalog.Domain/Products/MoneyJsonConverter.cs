using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopIt.Catalog.Domain.Products;

/// <summary>
/// Reads and writes <see cref="Money"/> as <c>{ "amount": "19.99", "currency": "EUR" }</c>.
/// </summary>
/// <remarks>
/// <para>
/// The amount is a JSON <em>string</em>, which is the point of ADR-0005: a JavaScript client parses a
/// JSON number as an IEEE-754 double, and <c>19.99</c> does not survive that round trip in general. A
/// string is carried through byte-for-byte.
/// </para>
/// <para>
/// Deserialization validates through <see cref="Money.Validate"/>, so a hand-written request cannot smuggle
/// a negative or over-precise amount past the aggregate. It throws <see cref="JsonException"/> rather than
/// returning an <c>Error</c> because this runs inside <c>System.Text.Json</c>, which has no result type —
/// which is also why the product endpoints take the amount as a string and map it to a field-level
/// <c>Result</c> failure themselves.
/// </para>
/// </remarks>
public sealed class MoneyJsonConverter : JsonConverter<Money>
{
    private const string AmountProperty = "amount";
    private const string CurrencyProperty = "currency";

    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException(
                $"Money must be an object like {{ \"{AmountProperty}\": \"19.99\", \"{CurrencyProperty}\": \"EUR\" }}.");
        }

        string? amountText = null;
        string? currencyCode = null;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                break;
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case AmountProperty:
                    if (reader.TokenType is not JsonTokenType.String)
                    {
                        throw new JsonException(
                            $"Money '{AmountProperty}' must be a JSON string, not {reader.TokenType}, so the " +
                            "amount cannot be rounded by a double.");
                    }

                    amountText = reader.GetString();
                    break;

                case CurrencyProperty:
                    if (reader.TokenType is not JsonTokenType.String)
                    {
                        throw new JsonException($"Money '{CurrencyProperty}' must be a JSON string.");
                    }

                    currencyCode = reader.GetString();
                    break;

                default:
                    // Unknown members are skipped rather than rejected: the response shape may grow and an
                    // older client must not fail on a field it does not know.
                    reader.Skip();
                    break;
            }
        }

        if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            throw new JsonException(
                $"Money '{AmountProperty}' is missing or is not a decimal written with the invariant culture.");
        }

        if (Money.Validate(amount, currencyCode) is { } error)
        {
            throw new JsonException(error.Description);
        }

        return Money.Create(amount, currencyCode);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(AmountProperty, value.Amount.ToString("0.00", CultureInfo.InvariantCulture));
        writer.WriteString(CurrencyProperty, value.CurrencyCode);
        writer.WriteEndObject();
    }
}
