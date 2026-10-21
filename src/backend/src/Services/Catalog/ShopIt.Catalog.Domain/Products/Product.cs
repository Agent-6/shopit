using ShopIt.Catalog.Domain.Events;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Providers;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Domain.Products;

/// <summary>
/// A tenant's product: a name, a SKU, an optional description, a price and an explicit lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// It implements <see cref="ITenantEntity"/>, so <c>CatalogDbContext</c> filters it by tenant and indexes
/// <c>TenantId</c>; its SKU is unique <em>within</em> a tenant. See <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </para>
/// <para>
/// There are no public setters. Everything a caller can change is an operation — <see cref="UpdateDetails"/>
/// and <see cref="ChangeStatus"/> — so the aggregate can enforce its invariants (a valid price, a SKU shape,
/// a legal status transition) and raise the matching domain event, instead of every handler re-deciding
/// them.
/// </para>
/// <para>
/// The price is <see cref="Money"/>, per ADR-0005. The currency is stamped from the platform-wide
/// <c>Catalog:Currency</c> setting until per-tenant currency exists (#84); the aggregate stores it with the
/// amount so the row stays interpretable.
/// </para>
/// <para>
/// Audit fields are stamped by the persistence convention (ADR-0004). The constructor records
/// <see cref="AuditedAggregateRoot{TId}.CreatedOn"/> so a create response can report it before the unit of
/// work commits; nothing here writes <c>CreatedBy</c> or <c>LastModified*</c>, because only the persistence
/// path knows who the actor is.
/// </para>
/// </remarks>
public class Product : AuditedAggregateRoot<Guid>, ITenantEntity
{
    /// <summary>The longest accepted name, in characters, once trimmed.</summary>
    public const int MaxNameLength = 200;

    /// <summary>The longest accepted SKU, in characters, once trimmed.</summary>
    public const int MaxSkuLength = 64;

    /// <summary>The longest accepted description, in characters, once trimmed.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>The name as entered, trimmed.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>The stock-keeping unit as entered, trimmed, with its original casing.</summary>
    public string Sku { get; private set; } = default!;

    /// <summary>
    /// The SKU's uniqueness key: <see cref="Sku"/> uppercased, stored as its own column.
    /// </summary>
    /// <remarks>
    /// The same shape <see cref="Categories.Category.NormalizedName"/> uses, for the same reason: "tee-001"
    /// and "TEE-001" are one product to a shop, a PostgreSQL unique index is case-sensitive, and EF Core
    /// cannot express a functional index on <c>upper(Sku)</c> in the model — so the key is a column.
    /// </remarks>
    public string NormalizedSku { get; private set; } = default!;

    /// <summary>The description, or <c>null</c> when the product has none.</summary>
    public string? Description { get; private set; }

    /// <summary>The price, in the currency this catalog prices in.</summary>
    public Money Price { get; private set; }

    /// <summary>The category this product belongs to, or <c>null</c> when it is uncategorised.</summary>
    /// <remarks>
    /// A plain id rather than a navigation property on purpose: the category is a separate aggregate, and
    /// loading it into the product would make a category's lifetime look like an invariant of the product's.
    /// The existence check is the command handler's job, under the same tenant filter.
    /// </remarks>
    public Guid? CategoryId { get; private set; }

    /// <summary>Where the product is in its lifecycle. Moved only by <see cref="ChangeStatus"/>.</summary>
    public ProductStatus Status { get; private set; }

    /// <summary>The tenant that owns this product; never empty.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Private parameterless constructor for Entity Framework Core.</summary>
    private Product() : base()
    {
    }

    private Product(
        Guid id,
        Guid tenantId,
        string name,
        string sku,
        string? description,
        Money price,
        Guid? categoryId) : base(id)
    {
        TenantId = tenantId;
        ApplyName(name);
        ApplySku(sku);
        ApplyDescription(description);
        Price = price;
        CategoryId = categoryId;
        Status = ProductStatus.Draft;

        CreatedOn = new DateTimeOffset(DomainProviders.Date.UtcNow, TimeSpan.Zero);
    }

    /// <summary>
    /// Creates a product owned by <paramref name="tenantId"/>, in <see cref="ProductStatus.Draft"/>.
    /// </summary>
    /// <remarks>
    /// Draft is not a parameter: a product that started life Active would be visible for sale the moment
    /// the request returned, so publishing is a separate, deliberate operation.
    /// </remarks>
    /// <param name="id">The new product's identifier.</param>
    /// <param name="tenantId">The owning tenant. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="name">The display name; trimmed and validated.</param>
    /// <param name="sku">The stock-keeping unit; trimmed and validated.</param>
    /// <param name="description">An optional description; trimmed, empty means none.</param>
    /// <param name="price">The validated price.</param>
    /// <param name="categoryId">An optional category, already checked to exist in this tenant.</param>
    /// <exception cref="ArgumentException">
    /// The tenant is empty, or a field violates its validation rule. Callers that can recover check first
    /// and never reach this.
    /// </exception>
    public static Product Create(
        Guid id,
        Guid tenantId,
        string name,
        string sku,
        string? description,
        Money price,
        Guid? categoryId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "A product must belong to a tenant; the catalog has no host-level products.",
                nameof(tenantId));
        }

        var product = new Product(id, tenantId, name, sku, description, price, categoryId);

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product));

        return product;
    }

    /// <summary>
    /// Replaces the product's editable details.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The status is not part of this operation — it moves through <see cref="ChangeStatus"/> only — so
    /// editing a product can never publish or withdraw it by accident.
    /// </para>
    /// <para>
    /// An archived product is refused: archive is the end of the lifecycle, and allowing an edit afterwards
    /// would leave a row that claims to be withdrawn carrying content nobody reviewed.
    /// </para>
    /// </remarks>
    /// <param name="name">The new name; trimmed and validated.</param>
    /// <param name="sku">The new SKU; trimmed and validated.</param>
    /// <param name="description">The new description, or <c>null</c> to remove it.</param>
    /// <param name="price">The new price.</param>
    /// <param name="categoryId">The new category, or <c>null</c> to make the product uncategorised.</param>
    /// <exception cref="InvalidOperationException">The product is archived.</exception>
    /// <exception cref="ArgumentException">A field violates its validation rule.</exception>
    public void UpdateDetails(
        string name,
        string sku,
        string? description,
        Money price,
        Guid? categoryId)
    {
        if (Status is ProductStatus.Archived)
        {
            throw new InvalidOperationException(ProductErrors.ArchivedIsImmutable(Id).Description);
        }

        ApplyName(name);
        ApplySku(sku);
        ApplyDescription(description);
        Price = price;
        CategoryId = categoryId;

        RaiseDomainEvent(new ProductUpdatedDomainEvent(this));
    }

    /// <summary>
    /// Moves the product to <paramref name="target"/> if the lifecycle allows it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Allowed transitions: <see cref="ProductStatus.Draft"/> to Active or Archived, and Active to Archived.
    /// Archived is terminal in M1 — restoring a withdrawn product needs a decision about its history and its
    /// price, so it is out of scope rather than implied.
    /// </para>
    /// <para>
    /// Asking for the status the product already has is a no-op that succeeds: a status endpoint is
    /// idempotent, so a client can send the desired state without reading first, and no event is raised for
    /// a change that did not happen.
    /// </para>
    /// </remarks>
    /// <param name="target">The status to move to.</param>
    /// <returns><c>true</c> when the status changed, <c>false</c> when it already was <paramref name="target"/>.</returns>
    /// <exception cref="InvalidOperationException">The transition is not allowed.</exception>
    public bool ChangeStatus(ProductStatus target)
    {
        if (Status == target)
        {
            return false;
        }

        if (!IsTransitionAllowed(Status, target))
        {
            throw new InvalidOperationException(ProductErrors.InvalidStatusTransition(Status, target).Description);
        }

        var previous = Status;
        Status = target;

        RaiseDomainEvent(new ProductStatusChangedDomainEvent(this, previous, target));

        return true;
    }

    /// <summary>
    /// Checks a candidate transition against the lifecycle, returning the failure instead of throwing it.
    /// </summary>
    /// <remarks>
    /// The single definition of the lifecycle: <see cref="ChangeStatus"/> throws what this returns and the
    /// command handler returns it as a <c>Result</c>, so the two cannot drift.
    /// </remarks>
    /// <param name="from">The current status.</param>
    /// <param name="to">The requested status.</param>
    /// <returns>An <see cref="Error"/> when the transition is not allowed, otherwise <c>null</c>.</returns>
    public static Error? ValidateStatusTransition(ProductStatus from, ProductStatus to) =>
        from == to || IsTransitionAllowed(from, to)
            ? null
            : ProductErrors.InvalidStatusTransition(from, to);

    /// <summary>Checks a candidate name against this aggregate's invariant.</summary>
    /// <param name="name">The candidate name, as received.</param>
    /// <returns>An <see cref="Error"/> when the name is invalid, otherwise <c>null</c>.</returns>
    public static Error? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.NameRequired();
        }

        return name.Trim().Length > MaxNameLength
            ? ProductErrors.NameTooLong(MaxNameLength)
            : null;
    }

    /// <summary>
    /// Checks a candidate SKU against this aggregate's invariant: required, bounded, and made of letters,
    /// digits and <c>-</c>, <c>_</c> or <c>.</c>.
    /// </summary>
    /// <remarks>
    /// The character rule exists because a SKU travels into URLs, CSV exports and barcode labels; spaces and
    /// slashes in one are a support ticket each. It is deliberately permissive about shape otherwise — the
    /// shop decides its own scheme.
    /// </remarks>
    /// <param name="sku">The candidate SKU, as received.</param>
    /// <returns>An <see cref="Error"/> when the SKU is invalid, otherwise <c>null</c>.</returns>
    public static Error? ValidateSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return ProductErrors.SkuRequired();
        }

        var trimmed = sku.Trim();

        if (trimmed.Length > MaxSkuLength)
        {
            return ProductErrors.SkuTooLong(MaxSkuLength);
        }

        return trimmed.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
            ? null
            : ProductErrors.SkuInvalidCharacters(trimmed);
    }

    /// <summary>Checks a candidate description against the stored maximum.</summary>
    /// <param name="description">The candidate description, as received.</param>
    /// <returns>An <see cref="Error"/> when the description is too long, otherwise <c>null</c>.</returns>
    public static Error? ValidateDescription(string? description) =>
        description is not null && description.Trim().Length > MaxDescriptionLength
            ? ProductErrors.DescriptionTooLong(MaxDescriptionLength)
            : null;

    /// <summary>
    /// The uniqueness key for a SKU: trimmed and uppercased with the invariant culture.
    /// </summary>
    /// <remarks>
    /// Public because the repository must compare SKUs the way the aggregate stores them; two
    /// implementations of "same SKU" is how a duplicate slips past a pre-check and fails at the index.
    /// </remarks>
    public static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    private static bool IsTransitionAllowed(ProductStatus from, ProductStatus to) =>
        (from, to) switch
        {
            (ProductStatus.Draft, ProductStatus.Active) => true,
            (ProductStatus.Draft, ProductStatus.Archived) => true,
            (ProductStatus.Active, ProductStatus.Archived) => true,
            _ => false
        };

    private void ApplyName(string name)
    {
        if (ValidateName(name) is { } error)
        {
            throw new ArgumentException(error.Description, nameof(name));
        }

        Name = name.Trim();
    }

    private void ApplySku(string sku)
    {
        if (ValidateSku(sku) is { } error)
        {
            throw new ArgumentException(error.Description, nameof(sku));
        }

        Sku = sku.Trim();
        NormalizedSku = NormalizeSku(Sku);
    }

    private void ApplyDescription(string? description)
    {
        if (ValidateDescription(description) is { } error)
        {
            throw new ArgumentException(error.Description, nameof(description));
        }

        // Whitespace is absence, not a description: an empty string and null both mean "none", so a client
        // cannot create two shapes of the same state.
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
