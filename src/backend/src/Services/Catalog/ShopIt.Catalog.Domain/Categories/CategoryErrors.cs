using ShopIt.Framework.Domain.Errors;

namespace ShopIt.Catalog.Domain.Categories;

/// <summary>
/// The expected failures of category operations, as values.
/// </summary>
/// <remarks>
/// <para>
/// "Not found", "name already taken" and "the name is not valid" are outcomes a caller can act on, so
/// they are <see cref="Error"/> values carried by a <c>Result</c> rather than exceptions that a
/// presentation layer has to catch and turn back into a status code (#32, #77). Argument violations
/// inside <see cref="Category"/> stay exceptions: those are bugs in the caller.
/// </para>
/// <para>
/// The <see cref="ErrorType"/> is the whole contract with the HTTP layer — <c>ToProblem()</c> maps it to
/// a status code — which is why each value below is chosen deliberately rather than defaulted.
/// </para>
/// </remarks>
public static class CategoryErrors
{
    /// <summary>The category does not exist in the acting tenant.</summary>
    /// <remarks>
    /// Deliberately <see cref="ErrorType.NotFound"/> and not "forbidden" when the id belongs to another
    /// tenant: the tenant query filter hides that row, so the answer must not confirm that the id exists
    /// elsewhere.
    /// </remarks>
    public static Error NotFound(Guid categoryId) =>
        new("Category.NotFound", $"A category with the id '{categoryId}' was not found.", ErrorType.NotFound);

    /// <summary>Another category in the same tenant already uses this name.</summary>
    /// <remarks>
    /// The uniqueness it reports is per tenant — two tenants may each own a "Tools" category — because
    /// the repository check runs under the tenant query filter and the unique index leads with
    /// <see cref="Category.TenantId"/>.
    /// </remarks>
    public static Error NameAlreadyExists(string name) =>
        new("Category.NameAlreadyExists", $"A category named '{name.Trim()}' already exists.", ErrorType.Conflict);

    /// <summary>The name was empty or whitespace.</summary>
    public static Error NameRequired() =>
        new("Category.NameRequired", "A category name is required.", ErrorType.Validation);

    /// <summary>The name exceeds the maximum stored length once trimmed.</summary>
    public static Error NameTooLong(int maxLength) =>
        new("Category.NameTooLong", $"A category name cannot exceed {maxLength} characters.", ErrorType.Validation);

    /// <summary>The request acts as the host, and the catalog is tenant-only.</summary>
    /// <remarks>
    /// <see cref="ErrorType.Forbidden"/> rather than a validation error: the request is well formed, the
    /// caller simply is not acting as a tenant. Refusing here matters beyond tidiness — at host scope the
    /// tenant query filter is disabled (ADR-0001), so an unguarded host-scope read would return every
    /// tenant's categories and an unguarded write would stamp <see cref="Guid.Empty"/> as the owner.
    /// </remarks>
    public static Error TenantRequired() =>
        new(
            "Category.TenantRequired",
            "The catalog is tenant-scoped: a category cannot be read or changed without a tenant.",
            ErrorType.Forbidden);

    /// <summary>The requested page number is below one.</summary>
    public static Error InvalidPageNumber() =>
        new("Category.InvalidPageNumber", "The page number must be 1 or greater.", ErrorType.Validation);

    /// <summary>The requested page size is outside 1..<paramref name="maxPageSize"/>.</summary>
    public static Error InvalidPageSize(int maxPageSize) =>
        new("Category.InvalidPageSize", $"The page size must be between 1 and {maxPageSize}.", ErrorType.Validation);
}
