using System.Text.Json;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Framework.Domain.Errors;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers ADR-0005's value object: the invariants that make a price valid, and the wire contract that keeps it
/// exact on the way to a JavaScript client.
/// </summary>
/// <remarks>
/// The rules under test are the ones the ADR says are expensive to change later — more than two decimals is
/// rejected rather than rounded, a price is never negative, and the JSON amount is a string so no double sits
/// between the database and the portal.
/// </remarks>
public class CatalogMoneyTests
{
    [Fact]
    public void A_price_with_three_decimal_places_is_rejected_rather_than_rounded()
    {
        var error = Money.Validate(19.999m, "EUR");

        Assert.NotNull(error);
        Assert.Equal("Product.InvalidPriceScale", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        // Rejected, not rounded: creating it must fail instead of producing 20.00.
        Assert.Throws<ArgumentException>(() => Money.Create(19.999m, "EUR"));
    }

    [Fact]
    public void A_negative_price_is_rejected()
    {
        var error = Money.Validate(-0.01m, "EUR");

        Assert.NotNull(error);
        Assert.Equal("Product.NegativePrice", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        Assert.Throws<ArgumentException>(() => Money.Create(-1m, "EUR"));
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("")]
    public void A_currency_code_that_is_not_three_letters_is_rejected(string currencyCode)
    {
        var error = Money.Validate(1m, currencyCode);

        Assert.NotNull(error);
        Assert.Equal("Product.InvalidCurrency", error.Code);
    }

    [Fact]
    public void A_missing_currency_code_is_rejected()
    {
        Assert.Equal("Product.InvalidCurrency", Money.Validate(1m, null)?.Code);
    }

    [Fact]
    public void A_valid_price_round_trips_through_the_json_converter()
    {
        var price = Money.Create(19.99m, "eur");

        // Normalized on the way in: casing is a client convenience, the stored form is uppercase.
        Assert.Equal("EUR", price.CurrencyCode);

        var json = JsonSerializer.Serialize(price);

        // The amount is a JSON string, which is the whole point: a double cannot round it in transit.
        Assert.Equal("""{"amount":"19.99","currency":"EUR"}""", json);

        var roundTripped = JsonSerializer.Deserialize<Money>(json);

        Assert.Equal(price, roundTripped);
        Assert.Equal(19.99m, roundTripped.Amount);
        Assert.Equal("EUR", roundTripped.CurrencyCode);
    }

    [Fact]
    public void The_json_converter_keeps_trailing_zeroes_so_a_price_reads_as_money()
    {
        var json = JsonSerializer.Serialize(Money.Create(14m, "EUR"));

        Assert.Equal("""{"amount":"14.00","currency":"EUR"}""", json);
    }

    [Fact]
    public void The_json_converter_refuses_a_numeric_amount()
    {
        // A JSON number would already have been through a double, so it is not accepted as money.
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Money>("""{"amount":19.99,"currency":"EUR"}"""));
    }

    [Fact]
    public void The_json_converter_refuses_an_invalid_amount()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Money>("""{"amount":"-1.00","currency":"EUR"}"""));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Money>("""{"amount":"1.234","currency":"EUR"}"""));
    }

    [Fact]
    public void Two_amounts_of_money_are_equal_by_value()
    {
        // The aggregate, the converter and the tests all compare prices; value equality is what makes that
        // meaningful rather than reference identity.
        Assert.Equal(Money.Create(5.50m, "EUR"), Money.Create(5.50m, "EUR"));
        Assert.NotEqual(Money.Create(5.50m, "EUR"), Money.Create(5.50m, "USD"));
    }
}
