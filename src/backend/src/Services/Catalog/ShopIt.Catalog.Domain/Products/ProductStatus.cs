using System.Text.Json.Serialization;

namespace ShopIt.Catalog.Domain.Products;

/// <summary>
/// A product's position in its lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The lifecycle is deliberately explicit: a product is created <see cref="Draft"/> and moves only through
/// <see cref="Product.ChangeStatus"/>, never by a writable property. That is what lets a future consumer
/// (a storefront, a price list) rely on "Active means published" without every write path being audited for
/// free-form status writes.
/// </para>
/// <para>
/// It serialises as its name (<c>"Active"</c>, not <c>1</c>) so the HTTP contract survives renumbering the
/// enum and a client can read the response without a lookup table.
/// </para>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ProductStatus>))]
public enum ProductStatus
{
    /// <summary>Created but not for sale yet. The only status a product is created in.</summary>
    Draft = 0,

    /// <summary>For sale.</summary>
    Active = 1,

    /// <summary>Withdrawn. Terminal in M1: an archived product is not restored.</summary>
    Archived = 2
}
