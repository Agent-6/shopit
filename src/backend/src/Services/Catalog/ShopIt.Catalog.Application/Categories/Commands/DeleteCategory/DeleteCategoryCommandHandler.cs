using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Categories.Commands.DeleteCategory;

/// <summary>
/// Handles <see cref="DeleteCategoryCommand"/>: find the category, refuse if products still use it, then
/// remove it.
/// </summary>
public class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    ICurrentTenant currentTenant)
    : ICommandHandler<DeleteCategoryCommand, Result<Unit, Error>>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<Unit, Error>> HandleAsync(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        // Writes are refused at host scope: there is no tenant to own the row (ADR-0001).
        if (_currentTenant.IsHost)
        {
            return CategoryErrors.TenantRequired();
        }

        // Find, not Get: another tenant's id is reported as not found rather than as an exception.
        var category = await _categoryRepository.FindByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(request.Id);
        }

        // Tenant-filtered, so only this tenant's products can block the delete. The foreign key is
        // restrict-on-delete as a backstop for the race this check cannot close.
        if (await _productRepository.AnyByCategoryAsync(category.Id, cancellationToken))
        {
            return CategoryErrors.CategoryHasProducts(category.Id);
        }

        await _categoryRepository.RemoveAsync(category, cancellationToken);

        return Unit.Value;
    }
}
