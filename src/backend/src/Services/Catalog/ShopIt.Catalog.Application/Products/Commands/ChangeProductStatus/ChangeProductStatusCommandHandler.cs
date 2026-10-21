using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.Catalog.Application.Products.Commands.ChangeProductStatus;

/// <summary>
/// Handles <see cref="ChangeProductStatusCommand"/>: find the product, check the transition against the
/// lifecycle, then apply it.
/// </summary>
public class ChangeProductStatusCommandHandler(
    IProductRepository productRepository,
    ICurrentTenant currentTenant)
    : ICommandHandler<ChangeProductStatusCommand, Result<ChangeProductStatusResult, Error>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public async Task<Result<ChangeProductStatusResult, Error>> HandleAsync(
        ChangeProductStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Writes are refused at host scope: there is no tenant to own the row (ADR-0001).
        if (_currentTenant.IsHost)
        {
            return ProductErrors.TenantRequired();
        }

        var product = await _productRepository.FindByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

        var previousStatus = product.Status;

        // Same rule, same place as the aggregate's: a transition the lifecycle does not allow is a
        // field-level 400 here instead of the exception Product.ChangeStatus throws for callers that
        // skipped the check.
        if (Product.ValidateStatusTransition(previousStatus, request.Status) is { } transitionError)
        {
            return transitionError;
        }

        var changed = product.ChangeStatus(request.Status);

        // A no-op needs no write: the unit of work would otherwise stamp LastModifiedOn for a request that
        // changed nothing, which is exactly the audit trail a reader cannot trust.
        if (changed)
        {
            await _productRepository.UpdateAsync(product, cancellationToken);
        }

        return new ChangeProductStatusResult(
            Id: product.Id,
            PreviousStatus: previousStatus,
            Status: product.Status,
            Changed: changed,
            TenantId: product.TenantId,
            LastModifiedOn: product.LastModifiedOn);
    }
}
