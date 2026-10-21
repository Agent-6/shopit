using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Categories.Queries.GetCategory;

/// <summary>
/// Handles <see cref="GetCategoryQuery"/>.
/// </summary>
public class GetCategoryQueryHandler(
    ICategoryRepository categoryRepository,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetCategoryQuery, Result<GetCategoryResult, Error>>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<GetCategoryResult, Error>> HandleAsync(
        GetCategoryQuery request,
        CancellationToken cancellationToken)
    {
        // A host-scope read would return another tenant's category, because host scope disables the
        // query filter (ADR-0001). The catalog is tenant-only, so it is refused rather than answered.
        if (_currentTenant.IsHost)
        {
            return CategoryErrors.TenantRequired();
        }

        var category = await _categoryRepository.FindByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound(request.Id);
        }

        return new GetCategoryResult(
            Id: category.Id,
            Name: category.Name,
            TenantId: category.TenantId,
            CreatedOn: category.CreatedOn,
            LastModifiedOn: category.LastModifiedOn);
    }
}
