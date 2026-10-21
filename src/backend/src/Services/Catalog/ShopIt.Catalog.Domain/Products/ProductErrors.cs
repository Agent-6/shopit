using ShopIt.Framework.Domain.Errors;

namespace ShopIt.Catalog.Domain.Products;

/// <summary>
/// The expected failures of product operations, as values.
/// </summary>
/// <remarks>
/// <para>
/// The same rule as <see cref="Categories.CategoryErrors"/>: outcomes a caller can act on — not found,
/// SKU already taken, an invalid price, a category that does not exist in this tenant — are
/// <see cref="Error"/> values carried by a <c>Result</c>. Argument violations inside <see cref="Product"/>
/// stay exceptions: those are bugs in the caller.
/// </para>
/// <para>
/// A product may reference a category, so "the category still has products" is also a failure a caller can
/// act on — but it belongs to the <em>category</em> operation that trips over it and is defined once, in
/// <see cref="Categories.CategoryErrors.CategoryHasProducts"/>, rather than duplicated here under a second
/// code.
/// </para>
/// </remarks>
public static class ProductErrors
{
    /// <summary>The product does not exist in the acting tenant.</summary>
    /// <remarks>
    /// Deliberately <see cref="ErrorType.NotFound"/> and not "forbidden" when the id belongs to another
    /// tenant: the tenant query filter hides that row, so the answer must not confirm that the id exists
    /// elsewhere.
    /// </remarks>
    public static Error NotFound(Guid productId) =>
        new("Product.NotFound", $"A product with the id '{productId}' was not found.", ErrorType.NotFound);

    /// <summary>Another product in the same tenant already uses this SKU.</summary>
    /// <remarks>
    /// The uniqueness it reports is per tenant — two tenants may each sell <c>TEE-001</c> — because the
    /// check runs under the tenant query filter and the unique index leads with
    /// <see cref="Product.TenantId"/>.
    /// </remarks>
    public static Error SkuAlreadyExists(string sku) =>
        new("Product.SkuAlreadyExists", $"A product with the SKU '{Product.NormalizeSku(sku)}' already exists.", ErrorType.Conflict);

    /// <summary>
    /// The supplied category id is not a category of the acting tenant.
    /// </summary>
    /// <remarks>
    /// <see cref="ErrorType.Validation"/> — a 400 — rather than <see cref="ErrorType.NotFound"/>, because the
    /// failing thing is a <em>field of the product being submitted</em>, not the product or the route. A 404
    /// on <c>POST /products</c> would read as "the product you are creating does not exist", and the tenant
    /// filter makes "no such category" and "another tenant's category" the same answer on purpose, so there
    /// is nothing a 404 would add.
    /// </remarks>
    public static Error CategoryNotFound(Guid categoryId) =>
        new(
            "Product.CategoryNotFound",
            $"A category with the id '{categoryId}' was not found in this catalog.",
            ErrorType.Validation);

    /// <summary>An archived product cannot be changed; archive ends the lifecycle for M1.</summary>
    public static Error ArchivedIsImmutable(Guid productId) =>
        new(
            "Product.ArchivedIsImmutable",
            $"The product '{productId}' is archived and can no longer be changed.",
            ErrorType.Conflict);

    /// <summary>The requested status change is not a transition the lifecycle allows.</summary>
    public static Error InvalidStatusTransition(ProductStatus from, ProductStatus to) =>
        new(
            "Product.InvalidStatusTransition",
            $"A product cannot go from '{from}' to '{to}'. Allowed: Draft to Active or Archived, Active to Archived.",
            ErrorType.Validation);

    /// <summary>The name was empty or whitespace.</summary>
    public static Error NameRequired() =>
        new("Product.NameRequired", "A product name is required.", ErrorType.Validation);

    /// <summary>The name exceeds the maximum stored length once trimmed.</summary>
    public static Error NameTooLong(int maxLength) =>
        new("Product.NameTooLong", $"A product name cannot exceed {maxLength} characters.", ErrorType.Validation);

    /// <summary>The SKU was empty or whitespace.</summary>
    public static Error SkuRequired() =>
        new("Product.SkuRequired", "A product SKU is required.", ErrorType.Validation);

    /// <summary>The SKU exceeds the maximum stored length.</summary>
    public static Error SkuTooLong(int maxLength) =>
        new("Product.SkuTooLong", $"A product SKU cannot exceed {maxLength} characters.", ErrorType.Validation);

    /// <summary>The SKU contains a character a SKU may not contain.</summary>
    public static Error SkuInvalidCharacters(string sku) =>
        new(
            "Product.SkuInvalidCharacters",
            $"The SKU '{sku}' may only contain letters, digits and '-', '_' or '.'.",
            ErrorType.Validation);

    /// <summary>The description exceeds the maximum stored length.</summary>
    public static Error DescriptionTooLong(int maxLength) =>
        new(
            "Product.DescriptionTooLong",
            $"A product description cannot exceed {maxLength} characters.",
            ErrorType.Validation);

    /// <summary>The price is negative. A refund is not a price.</summary>
    public static Error NegativePrice() =>
        new("Product.NegativePrice", "A price cannot be negative.", ErrorType.Validation);

    /// <summary>The price has more decimal places than the currency's minor unit allows.</summary>
    public static Error InvalidPriceScale(int scale) =>
        new(
            "Product.InvalidPriceScale",
            $"A price cannot have more than {scale} decimal places; it is rejected rather than rounded.",
            ErrorType.Validation);

    /// <summary>The currency is not a three-letter ISO 4217 code.</summary>
    public static Error InvalidCurrency(string? currencyCode) =>
        new(
            "Product.InvalidCurrency",
            currencyCode is null or ""
                ? "A price requires an ISO 4217 currency code."
                : $"'{currencyCode}' is not an ISO 4217 currency code.",
            ErrorType.Validation);

    /// <summary>
    /// The request names a currency the platform does not use for this tenant.
    /// </summary>
    /// <remarks>
    /// Until per-tenant currency exists (#84) the catalog stamps the single configured currency on every
    /// price, so a request that names another one is refused rather than quietly re-stamped — a price of
    /// <c>19.99 USD</c> silently stored as <c>19.99 EUR</c> would be a financial error.
    /// </remarks>
    public static Error CurrencyMismatch(string expected, string? actual) =>
        new(
            "Product.CurrencyMismatch",
            $"This catalog prices products in '{expected}'; the request used '{actual}'. " +
            "Per-tenant currency is not supported yet (#84).",
            ErrorType.Validation);

    /// <summary>A price was not supplied, or its amount could not be read as a decimal.</summary>
    public static Error PriceRequired() =>
        new(
            "Product.PriceRequired",
            "A price is required, as { \"amount\": \"19.99\", \"currency\": \"EUR\" } with the amount as a string.",
            ErrorType.Validation);

    /// <summary>A status was not supplied with the status-change request.</summary>
    public static Error StatusRequired() =>
        new(
            "Product.StatusRequired",
            $"A target status is required: one of {string.Join(", ", Enum.GetNames<ProductStatus>())}.",
            ErrorType.Validation);

    /// <summary>The requested page number is below one.</summary>
    /// <remarks>
    /// A <c>Result</c> failure rather than a clamp to page 1: silently answering a different page than the one
    /// asked for hides a client bug and makes paging loops that never advance look like they work.
    /// </remarks>
    public static Error InvalidPageNumber() =>
        new("Product.InvalidPageNumber", "The page number must be 1 or greater.", ErrorType.Validation);

    /// <summary>The requested page size is outside 1..<paramref name="maxPageSize"/>.</summary>
    /// <remarks>
    /// Rejected rather than capped: an uncapped page size lets one client ask for the whole catalog, and a
    /// silently capped one makes the client believe it received everything it asked for.
    /// </remarks>
    public static Error InvalidPageSize(int maxPageSize) =>
        new("Product.InvalidPageSize", $"The page size must be between 1 and {maxPageSize}.", ErrorType.Validation);

    /// <summary>The requested sort column is not one the product list supports.</summary>
    /// <remarks>
    /// Unknown keys are refused instead of defaulting to the name order. A typo (<c>sortBy=prcie</c>) that
    /// silently returns the default order is a client bug the API helped create.
    /// </remarks>
    /// <param name="sortBy">The key the request supplied.</param>
    /// <param name="supported">The keys that are accepted, for the message.</param>
    public static Error InvalidSortBy(string? sortBy, IEnumerable<string> supported) =>
        new(
            "Product.InvalidSortBy",
            $"'{sortBy}' is not a sortable product field. Supported: {string.Join(", ", supported)}.",
            ErrorType.Validation);

    /// <summary>The requested sort direction is neither ascending nor descending.</summary>
    /// <param name="sortOrder">The value the request supplied.</param>
    /// <param name="supported">The values that are accepted, for the message.</param>
    public static Error InvalidSortOrder(string? sortOrder, IEnumerable<string> supported) =>
        new(
            "Product.InvalidSortOrder",
            $"'{sortOrder}' is not a sort direction. Supported: {string.Join(", ", supported)}.",
            ErrorType.Validation);

    /// <summary>The request acts as the host, and the catalog is tenant-only.</summary>
    /// <remarks>
    /// <see cref="ErrorType.Forbidden"/> rather than a validation error: the request is well formed, the
    /// caller simply is not acting as a tenant. At host scope the tenant query filter is disabled and a new
    /// product would be stamped with no owner (ADR-0001).
    /// </remarks>
    public static Error TenantRequired() =>
        new(
            "Product.TenantRequired",
            "The catalog is tenant-scoped: a product cannot be read or changed without a tenant.",
            ErrorType.Forbidden);

    /// <summary>
    /// A single validation failure with a message this service owns.
    /// </summary>
    /// <remarks>
    /// The Identity-style shape: expected field-level validation is a value, not a thrown exception, so the
    /// presentation layer turns it into a 400 with the domain's own wording. Used where the failure has no
    /// dedicated factory of its own — a supplied-but-empty id, for instance.
    /// </remarks>
    public static Error Invalid(string code, string description) =>
        new(code, description, ErrorType.Validation);
}
