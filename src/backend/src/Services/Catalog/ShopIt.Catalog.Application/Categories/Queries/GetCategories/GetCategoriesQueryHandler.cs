using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Categories.Queries.GetCategories;

/// <summary>
/// Handles <see cref="GetCategoriesQuery"/>.
/// </summary>
public class GetCategoriesQueryHandler(
    ICategoryRepository categoryRepository,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetCategoriesQuery, Result<GetCategoriesResult, Error>>
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<GetCategoriesResult, Error>> HandleAsync(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        // Host scope disables the tenant filter, so an unguarded list would return every tenant's
        // catalog (ADR-0001). Refused rather than filtered, because the catalog has no host view.
        if (_currentTenant.IsHost)
        {
            return CategoryErrors.TenantRequired();
        }

        if (request.PageNumber < 1)
        {
            return CategoryErrors.InvalidPageNumber();
        }

        if (request.PageSize is < 1 or > GetCategoriesQuery.MaxPageSize)
        {
            return CategoryErrors.InvalidPageSize(GetCategoriesQuery.MaxPageSize);
        }

        var (categories, totalCount, totalPages) = await _categoryRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Filter,
            cancellationToken);

        var items = categories.Select(category => new GetCategoriesCategoryItem(
            Id: category.Id,
            Name: category.Name,
            CreatedOn: category.CreatedOn,
            LastModifiedOn: category.LastModifiedOn));

        return new GetCategoriesResult(
            Items: items,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages);
    }
}
