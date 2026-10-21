namespace ShopIt.Catalog.Domain.Products;

/// <summary>
/// The direction of a product list's order.
/// </summary>
/// <remarks>
/// A two-valued enum rather than a boolean because "ascending" is the default in every client's head and a
/// parameter called <c>descending</c> makes the call site read as a double negative.
/// </remarks>
public enum ProductSortOrder
{
    /// <summary>Smallest/lowest first; the default.</summary>
    Ascending = 0,

    /// <summary>Largest/highest first.</summary>
    Descending = 1
}
