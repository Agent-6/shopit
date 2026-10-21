using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Tenancy.Client.Services;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// A <see cref="ITenantCurrencyProvider"/> whose answer a test decides, so the tenant registry is never on the far
/// side of a unit test.
/// </summary>
/// <remarks>
/// <para>
/// <strong>It fails for a tenant it was not told about</strong>, rather than defaulting to <c>EUR</c>. A default would
/// let a test that forgot to describe its tenant pass while proving nothing about where the currency came from — and
/// the one behaviour #84 exists to establish is that the value arrives from the registry rather than from a setting.
/// </para>
/// <para>
/// <see cref="Failure"/> overrides every answer, which is how "the registry cannot be reached" is expressed: the
/// caller must refuse the write instead of falling back.
/// </para>
/// </remarks>
internal sealed class FakeTenantCurrencyProvider : ITenantCurrencyProvider
{
    private readonly Dictionary<Guid, string> _currencies = [];

    /// <summary>Every tenant the provider was asked about, in order.</summary>
    public List<Guid> Requests { get; } = [];

    /// <summary>When set, every lookup fails with this error instead of answering.</summary>
    public Error? Failure { get; set; }

    /// <summary>Describes a tenant's currency.</summary>
    public FakeTenantCurrencyProvider With(Guid tenantId, string currency)
    {
        _currencies[tenantId] = currency;

        return this;
    }

    /// <summary>Starts failing every lookup, as an unreachable registry would.</summary>
    public FakeTenantCurrencyProvider Unavailable()
    {
        Failure = TenantCurrencyErrors.Unavailable(Guid.Empty);

        return this;
    }

    public Task<Result<string, Error>> GetCurrencyAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        Requests.Add(tenantId);

        if (Failure is { } failure)
        {
            return Task.FromResult<Result<string, Error>>(failure);
        }

        return Task.FromResult(_currencies.TryGetValue(tenantId, out var currency)
            ? Result<string, Error>.Success(currency)
            : TenantCurrencyErrors.TenantNotFound(tenantId));
    }
}
