using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Domain.Entities;
using ShopIt.Tenancy.Domain.Repositories;
using ShopIt.Tenancy.Domain.Tenants;

namespace ShopIt.Tenancy.Application.Tenants.Commands.ChangeTenantCurrency;

/// <summary>
/// Handles <see cref="ChangeTenantCurrencyCommand"/>: find the tenant, validate the code, then change it.
/// </summary>
/// <remarks>
/// The catalog's cached copy of this value is deliberately not invalidated from here. The cache is a short-lived
/// in-memory entry inside the catalog process (ADR-0006), not the shared cache, precisely so that resolving a
/// currency on a write path cannot silently serve a value the fail-safe kept alive for hours; the cost of that
/// choice is that this change is observed within the cache's lifetime rather than instantly.
/// </remarks>
public class ChangeTenantCurrencyCommandHandler(ITenantRepository tenantRepository)
    : ICommandHandler<ChangeTenantCurrencyCommand, Result<ChangeTenantCurrencyResult, Error>>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;

    public async Task<Result<ChangeTenantCurrencyResult, Error>> HandleAsync(
        ChangeTenantCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.FindByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound(request.Id);
        }

        // The aggregate's own rule, reported as a Result instead of the ArgumentException it throws for callers
        // that skipped this check — so there is one definition of a valid currency code.
        if (Tenant.ValidateCurrency(request.Currency) is { } invalidCurrency)
        {
            return invalidCurrency;
        }

        var previousCurrency = tenant.Currency;
        var changed = tenant.ChangeCurrency(request.Currency!);

        // A no-op needs no write: the unit of work would otherwise stamp LastModifiedOn for a request that
        // changed nothing, which is the audit trail a reader cannot trust.
        if (changed)
        {
            // Audit fields are stamped by the persistence path (ADR-0004), not here.
            await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        }

        return new ChangeTenantCurrencyResult(
            Id: tenant.Id,
            PreviousCurrency: previousCurrency,
            Currency: tenant.Currency,
            Changed: changed,
            LastModifiedOn: tenant.LastModifiedOn);
    }
}
