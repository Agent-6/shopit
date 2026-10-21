using ShopIt.Framework.Domain.Entities;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Providers;
using ShopIt.Tenancy.Domain.Events;
using ShopIt.Tenancy.Domain.Tenants;

namespace ShopIt.Tenancy.Domain.Entities;

/// <summary>
/// Represents a Tenant in the system.
/// Tenants are isolated organizational units with their own identifiers, name, currency and lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The tenant <em>is</em> where the platform's money unit lives: ADR-0005 says a price is denominated in its
/// tenant's currency, and it is this row that decides it. The catalog does not read this table — it is a
/// different service with its own database — so the value leaves Tenancy through the internal API recorded in
/// ADR-0006, and the catalog caches it.
/// </para>
/// <para>
/// Changing the currency does not re-price anything. Prices are historical facts stored with the currency they
/// were quoted in, so an existing product keeps its currency and only new writes use the new one; a reader that
/// wants to show both gets them from the catalog's read API. Re-pricing a catalog is a separate, explicit
/// operation that does not exist yet.
/// </para>
/// </remarks>
public class Tenant : AuditedAggregateRoot<Guid>
{
    /// <summary>The longest accepted name, in characters, once trimmed.</summary>
    public const int MaxNameLength = 100;

    /// <summary>How many letters an ISO 4217 currency code has.</summary>
    public const int CurrencyCodeLength = 3;

    /// <summary>
    /// The currency a tenant is created with, and the one every tenant that predates per-tenant currency was
    /// migrated to.
    /// </summary>
    /// <remarks>
    /// <c>EUR</c> because it is what the platform priced in before this existed (ADR-0005 seeded the demo tenant
    /// in euros), so the migration changes no price's meaning. It is the same value the database default uses.
    /// </remarks>
    public const string DefaultCurrency = "EUR";

    /// <summary>
    /// Gets the display name of the tenant.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Gets the tenant's currency: the ISO 4217 code every price this tenant writes is denominated in.
    /// </summary>
    /// <remarks>
    /// Upper case, always — <c>eur</c> and <c>EUR</c> are one currency, and storing both spellings would make
    /// "is this price in the tenant's currency" a comparison that can be wrong.
    /// </remarks>
    public string Currency { get; private set; } = DefaultCurrency;

    /// <summary>
    /// Gets a value indicating whether the tenant is active.
    /// </summary>
    public bool IsActive { get; private set; } = false;

    /// <summary>
    /// Private parameterless constructor for Entity Framework Core and JSON serialization.
    /// </summary>
    private Tenant() : base()
    {
    }

    /// <summary>
    /// Private constructor to enforce validation rules during creation.
    /// </summary>
    private Tenant(Guid id, string name) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty or whitespace.", nameof(name));

        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Tenant name cannot exceed {MaxNameLength} characters.", nameof(name));

        Name = name.Trim();
        Currency = DefaultCurrency;
        IsActive = false;

        // The aggregate records when it was created; the persistence convention fills in the actor and
        // anything else missing (ADR-0004). Without this the command result could only report a default
        // timestamp, because stamping happens after the handler returns.
        CreatedOn = new DateTimeOffset(DomainProviders.Date.UtcNow, TimeSpan.Zero);
    }

    /// <summary>
    /// Factory method to create a new Tenant instance.
    /// </summary>
    /// <param name="id">The unique identifier of the Tenant.</param>
    /// <param name="name">The display name of the Tenant.</param>
    /// <returns>A new Tenant instance.</returns>
    public static Tenant Create(Guid id, string name)
    {
        var tenant = new Tenant(id, name);
        
        tenant.RaiseDomainEvent(new TenantCreatedDomainEvent(tenant));
        
        return tenant;
    }

    /// <summary>
    /// Updates the Tenant's basic details.
    /// </summary>
    /// <param name="name">The new display name of the Tenant.</param>
    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty or whitespace.", nameof(name));

        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Tenant name cannot exceed {MaxNameLength} characters.", nameof(name));

        Name = name.Trim();

        RaiseDomainEvent(new TenantUpdatedDomainEvent(this));
    }

    /// <summary>
    /// Changes the currency every new price this tenant writes is denominated in.
    /// </summary>
    /// <remarks>
    /// Setting the currency it already has is a successful no-op that raises no event, so a client can send the
    /// state it wants without reading first. Nothing existing is converted: a stored price keeps the currency it
    /// was quoted in, which is why this operation is safe to expose while no re-pricing exists.
    /// </remarks>
    /// <param name="currency">The ISO 4217 code, any casing; stored upper case.</param>
    /// <returns><c>true</c> when the currency changed, <c>false</c> when it already was that one.</returns>
    /// <exception cref="ArgumentException">
    /// The code is not three ASCII letters. Callers that can recover validate with
    /// <see cref="ValidateCurrency"/> first and never reach this.
    /// </exception>
    public bool ChangeCurrency(string currency)
    {
        if (ValidateCurrency(currency) is { } error)
            throw new ArgumentException(error.Description, nameof(currency));

        var normalized = NormalizeCurrency(currency);

        if (string.Equals(Currency, normalized, StringComparison.Ordinal))
            return false;

        var previous = Currency;
        Currency = normalized;

        RaiseDomainEvent(new TenantCurrencyChangedDomainEvent(this, previous, Currency));

        return true;
    }

    /// <summary>
    /// Checks a candidate currency code against this aggregate's invariant, returning the failure instead of
    /// throwing it.
    /// </summary>
    /// <remarks>
    /// The single definition of "a valid tenant currency": <see cref="ChangeCurrency"/> throws what this returns
    /// and the command handler returns it as a <c>Result</c>. The shape rule (three ASCII letters, any casing) is
    /// deliberately the same as the catalog's <c>Money</c> value object: two slightly different ideas of what a
    /// currency code is would show up as a price the catalog refuses to stamp.
    /// </remarks>
    /// <param name="currency">The candidate code, as received.</param>
    /// <returns>An <c>Error</c> when the code is invalid, otherwise <c>null</c>.</returns>
    public static Error? ValidateCurrency(string? currency)
    {
        var candidate = currency?.Trim();

        return candidate is { Length: CurrencyCodeLength } && candidate.All(char.IsAsciiLetter)
            ? null
            : TenantErrors.InvalidCurrency(currency);
    }

    /// <summary>
    /// The stored form of a currency code: trimmed and upper case with the invariant culture.
    /// </summary>
    /// <remarks>
    /// Public because the command handler reports the normalized value, and two definitions of "the same
    /// currency code" is how a re-submitted <c>eur</c> looks like a change.
    /// </remarks>
    /// <param name="currency">The code, in any casing.</param>
    /// <returns>The code as it is stored.</returns>
    public static string NormalizeCurrency(string currency) => currency.Trim().ToUpperInvariant();

    /// <summary>
    /// Activates the Tenant.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;

        RaiseDomainEvent(new TenantActivatedDomainEvent(Id));
    }

    /// <summary>
    /// Deactivates the Tenant.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;

        RaiseDomainEvent(new TenantDeactivatedDomainEvent(Id));
    }
}
