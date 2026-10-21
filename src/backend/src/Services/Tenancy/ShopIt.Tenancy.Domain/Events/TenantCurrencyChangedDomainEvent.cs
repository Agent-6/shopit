using ShopIt.Framework.Domain.Events;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Domain.Events;

/// <summary>
/// Raised when a tenant's currency has changed.
/// </summary>
/// <remarks>
/// <para>
/// A change of money unit is a fact other services will eventually care about — a storefront showing what a
/// catalogue is priced in, a future re-pricing job. No handler consumes it yet, for the same reason the
/// product events are raised without one: the aggregate's vocabulary is part of its design, dispatch costs
/// nothing while no handler is registered, and the currency change would otherwise have to be inferred from
/// two <see cref="TenantUpdatedDomainEvent"/> snapshots that do not say which field moved.
/// </para>
/// <para>
/// The previous code is carried explicitly: a consumer that must react to the change needs to know what it moved
/// away from, and the tenant row no longer holds it.
/// </para>
/// </remarks>
/// <param name="Tenant">The tenant, in its new state.</param>
/// <param name="PreviousCurrency">The ISO 4217 code it was priced in before.</param>
/// <param name="Currency">The ISO 4217 code it is priced in now.</param>
public record TenantCurrencyChangedDomainEvent(
    Tenant Tenant,
    string PreviousCurrency,
    string Currency) : DomainEvent;
