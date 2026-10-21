using ShopIt.Catalog.Domain.Events;
using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Providers;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Domain.Categories;

/// <summary>
/// A tenant's product category — the catalog service's first tenant-scoped aggregate.
/// </summary>
/// <remarks>
/// <para>
/// Flat by design for M1: nesting is out of scope (#77), so the aggregate is one entity with no
/// children.
/// </para>
/// <para>
/// It implements <see cref="ITenantEntity"/>, which is what gives it a <c>TenantId</c> query filter and
/// index from <c>CatalogDbContext.ApplyTenantFilters</c>. A category read under tenant A can therefore
/// never be tenant B's, and a category's name is unique <em>within</em> its tenant. See
/// <c>docs/adr/0001-tenant-isolation-model.md</c>.
/// </para>
/// <para>
/// Audit fields are stamped by the persistence convention (ADR-0004). The constructor records
/// <see cref="AuditedAggregateRoot{TId}.CreatedOn"/> — the framework's stamping fills only what is
/// missing and says why — so a create response can report it before the unit of work commits. Nothing
/// here writes <c>CreatedBy</c> or <c>LastModified*</c>: those are the acting user's, and only the
/// persistence path knows who that is.
/// </para>
/// </remarks>
public class Category : AuditedAggregateRoot<Guid>, ITenantEntity
{
    /// <summary>The longest accepted name, in characters, once trimmed.</summary>
    public const int MaxNameLength = 100;

    /// <summary>The name as entered, trimmed.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// The name's uniqueness key: <see cref="Name"/> uppercased for comparison, stored as its own column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two categories in one tenant may not differ only by case — "Tools" and "tools" are the same
    /// shelf in a shop, and a case-sensitive unique index would let both exist while the list endpoint's
    /// <c>ILIKE</c> search returned them as one result.
    /// </para>
    /// <para>
    /// A separate column rather than a functional index on <c>lower(Name)</c>: EF Core cannot express that
    /// functional index in the model, so the index would live only in a hand-written migration and vanish
    /// the first time someone regenerated the schema. This is the shape Identity already uses for
    /// <c>NormalizedName</c>.
    /// </para>
    /// </remarks>
    public string NormalizedName { get; private set; } = default!;

    /// <summary>The tenant that owns this category; never empty.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Private parameterless constructor for Entity Framework Core.</summary>
    private Category() : base()
    {
    }

    private Category(Guid id, Guid tenantId, string name) : base(id)
    {
        TenantId = tenantId;
        ApplyName(name);

        CreatedOn = new DateTimeOffset(DomainProviders.Date.UtcNow, TimeSpan.Zero);
    }

    /// <summary>
    /// Creates a category owned by <paramref name="tenantId"/>.
    /// </summary>
    /// <param name="id">The new category's identifier.</param>
    /// <param name="tenantId">The owning tenant. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="name">The display name; trimmed and validated.</param>
    /// <exception cref="ArgumentException">
    /// The tenant is empty, or the name violates <see cref="ValidateName"/>. Callers that can recover —
    /// a command handler mapping to a <c>Result</c> — check those first and never reach this.
    /// </exception>
    public static Category Create(Guid id, Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "A category must belong to a tenant; the catalog has no host-level categories.",
                nameof(tenantId));
        }

        var category = new Category(id, tenantId, name);

        category.RaiseDomainEvent(new CategoryCreatedDomainEvent(category));

        return category;
    }

    /// <summary>
    /// Renames the category.
    /// </summary>
    /// <remarks>
    /// Creation audit data is untouched: it describes the row, not this change. The modification stamps
    /// are written by the persistence convention when the unit of work commits.
    /// </remarks>
    /// <param name="name">The new display name; trimmed and validated.</param>
    /// <exception cref="ArgumentException">The name violates <see cref="ValidateName"/>.</exception>
    public void Rename(string name)
    {
        ApplyName(name);

        RaiseDomainEvent(new CategoryRenamedDomainEvent(this));
    }

    /// <summary>
    /// Checks a candidate name against this aggregate's invariant, returning the failure instead of
    /// throwing it.
    /// </summary>
    /// <remarks>
    /// The single definition of "a valid name": <see cref="ApplyName"/> throws what this returns, and the
    /// command handlers return it as a <c>Result</c>. Duplicating the rules in a FluentValidation
    /// validator would let the two drift — and the current validation pipeline is a pass-through, so a
    /// validator rule alone would not reject anything anyway.
    /// </remarks>
    /// <param name="name">The candidate name, as received.</param>
    /// <returns>An <see cref="Error"/> when the name is invalid, otherwise <c>null</c>.</returns>
    public static Error? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired();
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return CategoryErrors.NameTooLong(MaxNameLength);
        }

        return null;
    }

    /// <summary>
    /// The uniqueness key for a name: trimmed and uppercased with the invariant culture.
    /// </summary>
    /// <remarks>
    /// Public because the repository must compare names the same way the aggregate stores them; two
    /// implementations of "same name" is how a duplicate slips past a pre-check and fails at the index.
    /// </remarks>
    public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    private void ApplyName(string name)
    {
        if (ValidateName(name) is { } error)
        {
            throw new ArgumentException(error.Description, nameof(name));
        }

        Name = name.Trim();
        NormalizedName = NormalizeName(Name);
    }
}
