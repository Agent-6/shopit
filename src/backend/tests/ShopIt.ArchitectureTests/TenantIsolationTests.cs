using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.EntityFrameworkCore;
using ShopIt.Catalog.Application.Categories.Queries.GetCategories;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Persistence.Data;
using ShopIt.Catalog.Persistence.Repositories;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Outbox;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// ADR-0001's cross-tenant leak proof ([#80](https://github.com/Agent-6/shopit/issues/80)): the
/// framework's <c>TenantAwareDbContext</c> exercised by a second service, over real catalog data, as an
/// automated test.
/// </summary>
/// <remarks>
/// <para>
/// Until this test existed the filter was proven only against Identity's private copy of it, so the
/// framework base class every future service inherits had never isolated two tenants. ADR-0001's review
/// checklist records that requirement and marks it unenforceable for want of a test project; this is that
/// test.
/// </para>
/// <para>
/// <strong>The one case a single-context test cannot catch.</strong> EF Core caches the model per context
/// type. If the filter lambda were rooted anywhere but the context instance — a captured local, a method
/// parameter, a field on another object — it would be evaluated once, when the model was built, and the
/// first tenant seen would be baked in for the process's lifetime: no exception, no warning, just every
/// later query silently filtered by the wrong tenant. A test that queries one context instance under two
/// tenants passes in both the correct and the baked world, because that one instance is what the model was
/// built from. <see cref="Separate_contexts_for_A_and_B_prove_the_filter_re_evaluates_instead_of_baking_the_first_tenant_seen"/>
/// therefore carries the proof; the other tests cover the shapes it does not.
/// </para>
/// <para>
/// <strong>Provider, and what it does not reproduce.</strong> These run on EF Core's InMemory provider,
/// which needs no database and no Aspire stack. That is enough for this proof: the tenant filter is a
/// model artefact evaluated by the query pipeline per query, not a relational feature, and the framework's
/// model conventions — <c>TenantFilterGuardConvention</c> included — run for every provider, so the
/// filters in the model are the ones PostgreSQL would see. It is <em>not</em> enough to materialise
/// <see cref="Product"/>: the price is a <c>Money</c> complex type, and the InMemory provider's shaper
/// throws <see cref="KeyNotFoundException"/> for complex properties, so product assertions project to
/// scalar columns (and <see cref="ProductRepository"/> uses existence queries). <see cref="Category"/>,
/// which has no complex property, is materialised in full — so "a list of entities is filtered" is proven
/// on a real entity, and the identical filter mechanism is asserted for products through the same query
/// pipeline.
/// </para>
/// </remarks>
public class TenantIsolationTests
{
    private static readonly Guid TenantA = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = new("22222222-2222-2222-2222-222222222222");

    /// <summary>The catalog assemblies this service owns; the audit in <see cref="No_catalog_code_calls_IgnoreQueryFilters"/> scans them.</summary>
    private static readonly string[] CatalogAssemblyNames =
    [
        "ShopIt.Catalog.API",
        "ShopIt.Catalog.Application",
        "ShopIt.Catalog.Domain",
        "ShopIt.Catalog.Infrastructure",
        "ShopIt.Catalog.Persistence",
        "ShopIt.Catalog.Presentation",
    ];

    [Fact]
    public async Task A_product_created_in_tenant_A_is_invisible_to_a_query_in_tenant_B()
    {
        var database = new CatalogTestDatabase();
        var currentTenant = new FakeCurrentTenant();
        await using var context = database.CreateContext(currentTenant);

        Guid productId;
        Guid categoryId;

        using (currentTenant.Change(new TenantInfo(TenantA, "Tenant A")))
        {
            var category = Category.Create(Guid.NewGuid(), TenantA, "Apparel");
            var product = NewProduct(TenantA, "TEE-001");

            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            productId = product.Id;
            categoryId = category.Id;
        }

        using (currentTenant.Change(new TenantInfo(TenantB, "Tenant B")))
        {
            await AssertAnotherTenantsRowsAreInvisibleAsync(context, productId, categoryId);
        }

        // And back again: the ambient scope moves the filter in both directions, so B's emptiness above is
        // isolation rather than a query that never worked.
        using (currentTenant.Change(new TenantInfo(TenantA, "Tenant A")))
        {
            Assert.Equal([productId], await context.Products.Select(product => product.Id).ToListAsync());
            Assert.Equal([categoryId], (await context.Categories.ToListAsync()).Select(category => category.Id));
        }
    }

    [Fact]
    public async Task Separate_contexts_for_A_and_B_prove_the_filter_re_evaluates_instead_of_baking_the_first_tenant_seen()
    {
        var database = new CatalogTestDatabase();

        // Two context instances, each pinned to its own tenant, over one store — the shape ADR-0001
        // requires. Deliberate order: A builds the model (and, if the lambda were not rooted at the context,
        // would fix A forever), then B must still see only B, then A again on a third, later instance.
        var tenantA = new FakeCurrentTenant(new TenantInfo(TenantA, "Tenant A"));
        await using var contextA = database.CreateContext(tenantA);

        var productA = NewProduct(TenantA, "TEE-001");
        var categoryA = Category.Create(Guid.NewGuid(), TenantA, "Apparel");

        contextA.Products.Add(productA);
        contextA.Categories.Add(categoryA);
        await contextA.SaveChangesAsync();

        var tenantB = new FakeCurrentTenant(new TenantInfo(TenantB, "Tenant B"));
        await using var contextB = database.CreateContext(tenantB);

        await AssertAnotherTenantsRowsAreInvisibleAsync(contextB, productA.Id, categoryA.Id);

        // The same SKU in a second tenant is a different product: uniqueness is per tenant, and what B sees
        // is its own row rather than A's — so "sees nothing of A" is not passing by seeing nothing at all.
        var productB = NewProduct(TenantB, "TEE-001");
        contextB.Products.Add(productB);
        await contextB.SaveChangesAsync();

        var seenByB = await contextB.Products
            .Select(product => new { product.Id, product.TenantId })
            .ToListAsync();

        Assert.Single(seenByB);
        Assert.Equal(productB.Id, seenByB[0].Id);
        Assert.Equal(TenantB, seenByB[0].TenantId);

        var tenantAAgain = new FakeCurrentTenant(new TenantInfo(TenantA, "Tenant A"));
        await using var contextAAgain = database.CreateContext(tenantAAgain);

        var seenByAAgain = await contextAAgain.Products
            .Select(product => new { product.Id, product.TenantId })
            .ToListAsync();

        Assert.Single(seenByAAgain);
        Assert.Equal(productA.Id, seenByAAgain[0].Id);
        Assert.Equal(TenantA, seenByAAgain[0].TenantId);

        Assert.Equal([categoryA.Id], (await contextAAgain.Categories.ToListAsync()).Select(category => category.Id));
    }

    [Fact]
    public async Task Host_scope_sees_every_tenants_rows_because_ADR_0001_disables_the_filter_there()
    {
        // ADR-0001 records this as a negative consequence, deliberately and not as a bug: a principal with
        // no tenant_id claim is Host (Guid.Empty) and the filter is off, which is what host-wide
        // administrative work needs. Asserted rather than assumed, and paired with the next test, which
        // pins the guard that keeps the catalog from answering a request in this scope.
        var database = new CatalogTestDatabase();

        var tenantA = new FakeCurrentTenant(new TenantInfo(TenantA, "Tenant A"));
        await using (var contextA = database.CreateContext(tenantA))
        {
            contextA.Products.Add(NewProduct(TenantA, "TEE-001"));
            contextA.Categories.Add(Category.Create(Guid.NewGuid(), TenantA, "Apparel"));
            await contextA.SaveChangesAsync();
        }

        var tenantB = new FakeCurrentTenant(new TenantInfo(TenantB, "Tenant B"));
        await using (var contextB = database.CreateContext(tenantB))
        {
            contextB.Products.Add(NewProduct(TenantB, "MUG-001"));
            contextB.Categories.Add(Category.Create(Guid.NewGuid(), TenantB, "Kitchen"));
            await contextB.SaveChangesAsync();
        }

        var host = new FakeCurrentTenant(new TenantInfo(Guid.Empty, "Host"));
        await using var hostContext = database.CreateContext(host);

        var everyTenantsProduct = await hostContext.Products
            .Select(product => new { product.Id, product.TenantId })
            .ToListAsync();

        Assert.Equal(2, everyTenantsProduct.Count);
        Assert.Equal(
            [TenantA, TenantB],
            everyTenantsProduct.Select(product => product.TenantId).Order().ToList());

        Assert.Equal(2, (await hostContext.Categories.ToListAsync()).Count);
    }

    [Fact]
    public async Task The_catalog_refuses_host_scope_so_that_host_visibility_is_not_reachable_through_an_endpoint()
    {
        // The other half of the decision above: "host scope sees every tenant" is only acceptable because no
        // catalog use case is allowed to run in it — otherwise the disabled filter would be an endpoint
        // listing every tenant's catalog. Every catalog handler carries this guard; one is enough here to
        // pin the contract and the failure a caller gets.
        var database = new CatalogTestDatabase();
        var host = new FakeCurrentTenant(new TenantInfo(Guid.Empty, "Host"));
        await using var context = database.CreateContext(host);

        var handler = new GetCategoriesQueryHandler(new CategoryRepository(context), host);

        var result = await handler.HandleAsync(new GetCategoriesQuery(1, 20, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.TenantRequired", result.Error.Code);
    }

    [Fact]
    public async Task Every_tenant_scoped_entity_in_the_catalog_model_carries_a_query_filter()
    {
        // Model-level companion to the behavioural tests: it names the entities the filter has to cover, so
        // an aggregate added later without ITenantEntity appears here as a missing entry rather than as a
        // leak nobody noticed. TenantFilterGuardConvention already fails the model build for a missing
        // filter; this states which entities are in scope at all.
        var database = new CatalogTestDatabase();
        await using var context = database.CreateContext(new FakeCurrentTenant(new TenantInfo(TenantA, "Tenant A")));

        var tenantScopedWithFilter = context.Model
            .GetEntityTypes()
            .Where(entityType => typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            .Where(entityType => entityType.GetDeclaredQueryFilters().Any())
            .Select(entityType => entityType.ClrType.Name)
            .Order()
            .ToList();

        Assert.Equal(["Category", "Product"], tenantScopedWithFilter);

        // And the filter is the tenant predicate, not some unrelated one: a filter on, say, a soft-delete
        // flag would satisfy a "has a filter" check while isolating nothing.
        var productFilter = context.Model
            .FindEntityType(typeof(Product))!
            .GetDeclaredQueryFilters()
            .Single()
            .Expression;

        Assert.Contains("TenantId", productFilter!.ToString());
        Assert.Contains("Guid.Empty", productFilter.ToString());
    }

    [Fact]
    public async Task The_only_unfiltered_tables_in_the_catalog_model_are_the_host_level_messaging_tables()
    {
        // The catalogue of unfiltered tables, asserted rather than assumed. The catalog owns no host-level
        // domain entity yet — both of its aggregates are tenant data — so the unfiltered set is exactly the
        // framework's inbox and outbox: messaging infrastructure keyed by a globally unique event id, with
        // no tenant owner and processed at host scope, the same class of table as Tenancy's Tenant. Anything
        // else appearing here is either a tenant entity that lost its filter or a new entity that should
        // have implemented ITenantEntity, and both need a decision recorded in ADR-0001.
        var database = new CatalogTestDatabase();
        await using var context = database.CreateContext(new FakeCurrentTenant(new TenantInfo(TenantA, "Tenant A")));

        var unfiltered = context.Model
            .GetEntityTypes()
            .Where(entityType => !entityType.GetDeclaredQueryFilters().Any())
            .Select(entityType => entityType.ClrType.Name)
            .Order()
            .ToList();

        Assert.Equal([nameof(InboxMessage), nameof(OutboxMessage)], unfiltered);

        // And "no filter" means readable from any tenant scope, which is what those tables need.
        await using var tenantBContext = database.CreateContext(
            new FakeCurrentTenant(new TenantInfo(TenantB, "Tenant B")));

        Assert.Empty(await tenantBContext.Set<OutboxMessage>().ToListAsync());
    }

    [Fact]
    public void No_catalog_code_calls_IgnoreQueryFilters()
    {
        // ADR-0001 enumerates the seven IgnoreQueryFilters() call sites that exist in the repository and
        // records them as temporary debt; none of them is in this service. The catalog's list is therefore:
        //
        //   ShopIt.Catalog.API            — none
        //   ShopIt.Catalog.Application    — none (host scope is refused, not bypassed: CategoryErrors.TenantRequired)
        //   ShopIt.Catalog.Domain         — none
        //   ShopIt.Catalog.Infrastructure — none
        //   ShopIt.Catalog.Persistence    — none (CatalogDataSeeder enters its tenant with Change())
        //   ShopIt.Catalog.Presentation   — none
        //
        // A new one would bypass only the filter while leaving ambient scope and write-stamping alone, so a
        // row read across tenants could be written back under the acting tenant — which is why the ADR
        // reaches cross-tenant data with Change(new TenantInfo(Guid.Empty, "Host")) instead.
        var offenders = CatalogAssemblyNames
            .Select(name => Assembly.Load(new AssemblyName(name)))
            .SelectMany(assembly => IgnoreQueryFiltersSites(assembly)
                .Select(site => $"{assembly.GetName().Name}: {site}"))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_IgnoreQueryFilters_scan_finds_a_real_call()
    {
        // The control for the audit above. A scan that silently found nothing — a renamed method, a provider
        // change, a metadata reader that stops working — would make that test pass while proving nothing,
        // which is the fail-open shape ADR-0001 exists to prevent. The probe's call is compiled into this
        // assembly, so a working detector must see it.
        var database = new CatalogTestDatabase();
        using var context = database.CreateContext(new FakeCurrentTenant(new TenantInfo(TenantA, "Tenant A")));

        // Executed too, so the probe is not unreachable code the compiler could drop.
        Assert.NotNull(IgnoreQueryFiltersProbe(context));
        Assert.Contains("EntityFrameworkQueryableExtensions", IgnoreQueryFiltersSites(typeof(TenantIsolationTests).Assembly));
    }

    /// <summary>
    /// Asserts that the rows created for one tenant are invisible from a context acting as another: by list,
    /// by id and through a filter.
    /// </summary>
    /// <remarks>
    /// Product reads are projections and existence checks because the InMemory provider cannot materialise
    /// <see cref="Product"/> — see this class's remarks. The filter is applied to the query root either way,
    /// and <see cref="Category"/> is materialised in full so "a list of entities" is proven end to end.
    /// </remarks>
    private static async Task AssertAnotherTenantsRowsAreInvisibleAsync(
        CatalogDbContext context,
        Guid productId,
        Guid categoryId)
    {
        // By list.
        Assert.Empty(await context.Products.Select(product => product.Id).ToListAsync());
        Assert.Empty(await context.Categories.ToListAsync());

        // By id. A query, not DbSet.Find: Find consults the change tracker first and would return an entity
        // this context had loaded without ever running the filter — a real way to leak a row, but not the
        // one under test here.
        Assert.False(await context.Products.AnyAsync(product => product.Id == productId));
        Assert.Null(await context.Categories.SingleOrDefaultAsync(category => category.Id == categoryId));

        // Through a filter, and through the repositories the handlers actually use: a predicate that matches
        // the row exactly still returns nothing, because the tenant filter is applied in addition to it.
        Assert.False(await new ProductRepository(context).ExistsBySkuAsync("TEE-001"));
        Assert.False(await new CategoryRepository(context).ExistsByNameAsync("Apparel"));
    }

    /// <summary>
    /// The call the control test expects <see cref="IgnoreQueryFiltersSites"/> to find. It exists only for
    /// that: no catalog code may do this.
    /// </summary>
    private static IQueryable<Product> IgnoreQueryFiltersProbe(CatalogDbContext context) =>
        context.Products.IgnoreQueryFilters();

    /// <summary>
    /// Returns the declaring types of every <c>IgnoreQueryFilters</c> method referenced by
    /// <paramref name="assembly"/>.
    /// </summary>
    /// <remarks>
    /// Read from the assembly's metadata rather than its source, so the check needs no path back to the
    /// repository and keeps working wherever the build output lands. A member reference exists only because
    /// some method body calls it, so an empty result means no code in the assembly calls it.
    /// </remarks>
    private static IReadOnlyList<string> IgnoreQueryFiltersSites(Assembly assembly)
    {
        using var stream = File.OpenRead(assembly.Location);
        using var peReader = new PEReader(stream);

        if (!peReader.HasMetadata)
        {
            return [];
        }

        var reader = peReader.GetMetadataReader();
        var sites = new List<string>();

        foreach (var handle in reader.MemberReferences)
        {
            var reference = reader.GetMemberReference(handle);

            if (!reader.StringComparer.Equals(
                    reference.Name,
                    nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters)))
            {
                continue;
            }

            sites.Add(reference.Parent.Kind is HandleKind.TypeReference
                ? reader.GetString(reader.GetTypeReference((TypeReferenceHandle)reference.Parent).Name)
                : reference.Parent.Kind.ToString());
        }

        return sites;
    }

    private static Product NewProduct(Guid tenantId, string sku) =>
        Product.Create(
            Guid.NewGuid(),
            tenantId,
            $"{sku} product",
            sku,
            description: null,
            Money.Create(9.99m, "EUR"),
            categoryId: null);
}
