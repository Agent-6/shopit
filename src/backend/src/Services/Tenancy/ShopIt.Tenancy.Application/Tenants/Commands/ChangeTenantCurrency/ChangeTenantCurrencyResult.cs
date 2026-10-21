namespace ShopIt.Tenancy.Application.Tenants.Commands.ChangeTenantCurrency;

/// <summary>
/// The tenant's currency after the change.
/// </summary>
/// <remarks>
/// <see cref="PreviousCurrency"/> is reported rather than inferred, and <see cref="Changed"/> distinguishes "done"
/// from "it already was that currency" — the same shape the catalog's status change uses, so a client can tell a
/// real change from an idempotent re-submission.
/// </remarks>
/// <param name="Id">The tenant's identifier.</param>
/// <param name="PreviousCurrency">The currency it was priced in before; equal to <paramref name="Currency"/> for a no-op.</param>
/// <param name="Currency">The currency it is priced in now.</param>
/// <param name="Changed">Whether this request changed anything.</param>
/// <param name="LastModifiedOn">When it was last modified, stamped after this handler returns.</param>
public record ChangeTenantCurrencyResult(
    Guid Id,
    string PreviousCurrency,
    string Currency,
    bool Changed,
    DateTimeOffset? LastModifiedOn);
