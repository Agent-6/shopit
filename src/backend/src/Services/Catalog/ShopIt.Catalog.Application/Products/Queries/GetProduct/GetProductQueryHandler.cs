using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Products.Queries.GetProduct;

/// <summary>
/// Handles <see cref="GetProductQuery"/>.
/// </summary>
/// <remarks>
/// A product id that belongs to another tenant reads as "not found", not "forbidden": the repository query
/// runs under the tenant filter, so the row is invisible and the answer must not confirm that the id exists
/// elsewhere (ADR-0001). The handler turns the repository's <c>null</c> into
/// <see cref="ProductErrors.NotFound"/> — a domain failure with a 404 mapping — rather than passing
/// <c>null</c> to the response.
/// </remarks>
public class GetProductQueryHandler(
    IProductRepository productRepository,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetProductQuery, Result<GetProductResult, Error>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<GetProductResult, Error>> HandleAsync(
        GetProductQuery request,
        CancellationToken cancellationToken)
    {
        // At host scope the tenant filter is disabled, so this read would answer with another tenant's
        // product. The catalog is tenant-only, so it is refused rather than answered.
        if (_currentTenant.IsHost)
        {
            return ProductErrors.TenantRequired();
        }

        var product = await _productRepository.FindByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

        return new GetProductResult(
            Id: product.Id,
            Name: product.Name,
            Sku: product.Sku,
            Description: product.Description,
            Price: product.Price,
            CategoryId: product.CategoryId,
            Status: product.Status,
            TenantId: product.TenantId,
            CreatedOn: product.CreatedOn,
            LastModifiedOn: product.LastModifiedOn,
            // The actor is a user id, not a name: resolving it to a display name would be an Identity
            // lookup, and this read must not depend on the caller also holding `user.view` (#81 reports
            // the id rather than pretending it knows who that is).
            CreatedBy: product.CreatedBy,
            LastModifiedBy: product.LastModifiedBy);
    }
}
