using ShopIt.Framework.Core.CQRS.Commands;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Tenancy.Application.Tenants.Commands.ChangeTenantCurrency;

/// <summary>
/// Changes the currency a tenant's future prices are denominated in.
/// </summary>
/// <remarks>
/// <para>
/// A command of its own rather than an optional field on <see cref="Commands.UpdateTenant.UpdateTenantCommand"/>:
/// renaming a tenant and changing the unit of every price it will charge are different decisions, and folding the
/// second into the first would let a rename silently move money. It is also the operation #84's acceptance
/// criteria turn on, so it is reachable on its own route.
/// </para>
/// <para>
/// Nothing is re-priced. A stored price keeps the currency it was quoted in — that is the defined behaviour
/// recorded in ADR-0006 — so this changes what the <em>next</em> write uses.
/// </para>
/// </remarks>
/// <param name="Id">The tenant whose currency changes.</param>
/// <param name="Currency">The new ISO 4217 code, any casing; stored upper case.</param>
public record ChangeTenantCurrencyCommand(
    Guid Id,
    string? Currency) : ICommand<Result<ChangeTenantCurrencyResult, Error>>;
