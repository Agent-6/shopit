using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Persistence.Auditing;
using ShopIt.Tenancy.Domain.Entities;

namespace ShopIt.Tenancy.Persistence.Data;

/// <summary>
/// Registers the demo tenant that Identity seeds its demo users into.
/// </summary>
/// <remarks>
/// <para>
/// Identity has always seeded users whose <c>TenantId</c> is <see cref="SeededTenantId"/>, and the catalog has
/// seeded that tenant's sample products, but no tenant row existed to describe them: nothing read the registry, so
/// nothing noticed. The catalog now resolves a tenant's currency from it (ADR-0006), so a tenant that is not
/// registered cannot create a product — which is correct, and would have made the demo unusable. This is the missing
/// half of an existing assumption, not a new feature.
/// </para>
/// <para>
/// It writes through the <c>DbContext</c> rather than the repository on purpose: <see cref="Tenant.Create"/> raises
/// <see cref="Domain.Events.TenantCreatedDomainEvent"/>, whose handler publishes a provisioning event, and Identity
/// has already seeded this tenant's roles and users. Raising it here would ask Identity to provision a tenant it
/// created itself. Seeding is therefore a row, not a lifecycle event — the same reasoning as the catalog's seeder.
/// </para>
/// <para>
/// Idempotent per row: a database that already has the tenant is left alone, so a restart is a no-op.
/// </para>
/// </remarks>
public class TenantDataSeeder(TenancyDbContext dbContext, ILogger<TenantDataSeeder> logger)
{
    /// <summary>The tenant Identity seeds its demo users into; the catalog's sample data lives here too.</summary>
    public static readonly Guid SeededTenantId = new("B5D0C0E4-3A5B-4CDC-8D2A-7F1F6C9F5B4E");

    /// <summary>A display name for <see cref="SeededTenantId"/>.</summary>
    public const string SeededTenantName = "Seeded Tenant";

    private readonly TenancyDbContext _dbContext = dbContext;
    private readonly ILogger<TenantDataSeeder> _logger = logger;

    /// <summary>
    /// Ensures the demo tenant exists, priced in <see cref="Tenant.DefaultCurrency"/>.
    /// </summary>
    /// <remarks>
    /// The currency is the default rather than a parameter: it is the value every tenant that predates #84 was
    /// migrated to, so seeding it any other way would make the demo tenant disagree with its own products.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Tenants.AnyAsync(tenant => tenant.Id == SeededTenantId, cancellationToken))
        {
            return;
        }

        var tenant = Tenant.Create(SeededTenantId, SeededTenantName);

        // Active, because everything else that refers to this tenant — its users, its products — assumes a live
        // shop; a demo tenant that is inactive is a trap for the next reader.
        tenant.Activate();

        _dbContext.Tenants.Add(tenant);

        // Actor: none. Seeding has no interactive user, and ADR-0004's null means exactly that.
        AuditInfo.Apply(_dbContext, actorId: null);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded the demo tenant {TenantId} ({Name}) in {Currency}.",
            tenant.Id, tenant.Name, tenant.Currency);
    }
}
