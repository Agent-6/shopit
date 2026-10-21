using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Products.Queries.GetProducts;

/// <summary>
/// Handles <see cref="GetProductsQuery"/>: tenant scope, paging and sort validation, then one query for the
/// page and one for the total.
/// </summary>
/// <remarks>
/// Every bound is reported as a <c>Result</c> failure and none of them is clamped. A clamp answers a different
/// question than the one asked — <c>pageSize=500</c> silently becoming 100, <c>page=0</c> becoming 1 — and a
/// client cannot tell that from a server that honoured the request.
/// </remarks>
public class GetProductsQueryHandler(
    IProductRepository productRepository,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetProductsQuery, Result<GetProductsResult, Error>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<GetProductsResult, Error>> HandleAsync(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        // Host scope disables the tenant filter, so an unguarded list would return every tenant's catalog
        // (ADR-0001). Refused rather than filtered, because the catalog has no host view.
        if (_currentTenant.IsHost)
        {
            return ProductErrors.TenantRequired();
        }

        if (request.PageNumber < 1)
        {
            return ProductErrors.InvalidPageNumber();
        }

        if (request.PageSize is < 1 or > GetProductsQuery.MaxPageSize)
        {
            return ProductErrors.InvalidPageSize(GetProductsQuery.MaxPageSize);
        }

        if (!TryResolve(
                GetProductsQuery.SortFields,
                request.SortBy,
                GetProductsQuery.DefaultSortBy,
                out ProductSortField sortBy))
        {
            return ProductErrors.InvalidSortBy(request.SortBy, GetProductsQuery.SupportedSortBy);
        }

        if (!TryResolve(
                GetProductsQuery.SortOrders,
                request.SortOrder,
                GetProductsQuery.DefaultSortOrder,
                out ProductSortOrder sortOrder))
        {
            return ProductErrors.InvalidSortOrder(request.SortOrder, GetProductsQuery.SupportedSortOrder);
        }

        var (products, totalCount, totalPages) = await _productRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Filter,
            request.CategoryId,
            request.Status,
            sortBy,
            sortOrder,
            cancellationToken);

        var items = products.Select(product => new GetProductsProductItem(
            Id: product.Id,
            Name: product.Name,
            Sku: product.Sku,
            Description: product.Description,
            Price: product.Price,
            CategoryId: product.CategoryId,
            Status: product.Status,
            CreatedOn: product.CreatedOn,
            LastModifiedOn: product.LastModifiedOn));

        return new GetProductsResult(
            Items: items,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages);
    }

    /// <summary>
    /// Resolves a wire value against the accepted ones, treating absence as the default.
    /// </summary>
    /// <remarks>
    /// Null or whitespace means "the client did not ask", which is the default rather than an error: a query
    /// string built by appending optional parameters routinely omits them. Any other unseen value is the
    /// caller's mistake and is refused by the caller of this method.
    /// </remarks>
    private static bool TryResolve<TValue>(
        IReadOnlyDictionary<string, TValue> accepted,
        string? requested,
        string defaultValue,
        out TValue value)
    {
        var candidate = string.IsNullOrWhiteSpace(requested) ? defaultValue : requested.Trim();

        if (accepted.TryGetValue(candidate, out var resolved))
        {
            value = resolved;
            return true;
        }

        value = default!;
        return false;
    }
}
