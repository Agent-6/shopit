namespace ShopIt.Catalog.Domain.Products;

/// <summary>
/// A column a product list may be ordered by.
/// </summary>
/// <remarks>
/// Typed rather than a string because the repository's <c>switch</c> over these values is exhaustive: a new
/// sortable column cannot be added without deciding how it translates to SQL, whereas a string would fall
/// through to a default and quietly sort by the wrong thing.
/// </remarks>
public enum ProductSortField
{
    /// <summary>The display name, the default ordering.</summary>
    Name = 0,

    /// <summary>The SKU, compared through its normalized (uppercased) form.</summary>
    Sku = 1,

    /// <summary>The price amount.</summary>
    Price = 2,

    /// <summary>When the product was created.</summary>
    CreatedOn = 3
}
