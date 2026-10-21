namespace ShopIt.Catalog.Application.Categories.Commands.RenameCategory;

/// <summary>
/// The renamed category.
/// </summary>
/// <remarks>
/// <see cref="LastModifiedOn"/> and <see cref="LastModifiedBy"/> are stamped by the persistence
/// convention, which runs after the handler returns, so on this response they still hold the values from
/// before the rename — the created stamps are accurate immediately, the modified ones need a re-read. The
/// fields are kept so the shape matches the single-category query and a client does not have to care.
/// </remarks>
/// <param name="Id">The category's identifier.</param>
/// <param name="Name">The stored (trimmed) name.</param>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="CreatedOn">When the category was created; unchanged by a rename.</param>
/// <param name="LastModifiedOn">When it was last modified.</param>
public record RenameCategoryResult(
    Guid Id,
    string Name,
    Guid TenantId,
    DateTimeOffset CreatedOn,
    DateTimeOffset? LastModifiedOn);
