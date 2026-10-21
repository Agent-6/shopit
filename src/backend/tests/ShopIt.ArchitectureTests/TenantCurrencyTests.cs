using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using ShopIt.Catalog.Application.Products.Commands.CreateProduct;
using ShopIt.Catalog.Application.Products.Commands.UpdateProduct;
using ShopIt.Catalog.Application.Products.Queries.GetProduct;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Catalog.Infrastructure;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Framework.Presentation.Results;
using ShopIt.Tenancy.Application.Tenants.Commands.ChangeTenantCurrency;
using ShopIt.Tenancy.Client.Services;
using ShopIt.Tenancy.Domain.Entities;
using ShopIt.Tenancy.Domain.Events;
using ShopIt.Tenancy.Domain.Repositories;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers [#84](https://github.com/Agent-6/shopit/issues/84): the tenant's currency is the source of truth for what a
/// price is denominated in, and the transport that carries it is the one ADR-0006 records.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Scope, stated honestly.</strong> These are unit tests: the tenant registry is a fake, so what is proven is
/// the catalog's <em>behaviour given an answer</em> — that it stamps the tenant's currency, refuses a request naming
/// another, refuses the write when there is no answer, and reports both currencies on a detail read — plus the
/// Tenancy side of the same edge (the aggregate's rules, the change use case, and the policy that guards the internal
/// endpoint). What is not proven here is the HTTP hop between the two services; that is the live verification recorded
/// on the pull request.
/// </para>
/// <para>
/// The cache is tested directly because its whole value is a negative property — a failure is <em>not</em> remembered —
/// and a property nobody exercises is a property that quietly stops holding.
/// </para>
/// </remarks>
public class TenantCurrencyTests
{
    private static readonly Guid EuroTenant = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PoundTenant = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DollarTenant = new("33333333-3333-3333-3333-333333333333");

    // ---------------------------------------------------------------- the acceptance criteria

    [Fact]
    public async Task A_tenant_whose_currency_is_GBP_stamps_GBP_without_the_caller_supplying_one()
    {
        // The first acceptance criterion, as a test: nothing in the request mentions a currency.
        var handler = CreateHandler(PoundTenant, Currency().With(PoundTenant, "GBP"));

        var result = await handler.HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("GBP", result.Value.Price.CurrencyCode);
        Assert.Equal(PoundTenant, result.Value.TenantId);
    }

    [Fact]
    public async Task Two_tenants_with_different_currencies_each_get_their_own_and_neither_can_use_the_others()
    {
        var currency = Currency().With(PoundTenant, "GBP").With(DollarTenant, "USD");
        var repository = new FakeProductRepository();

        var pounds = await CreateHandler(PoundTenant, currency, repository).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, null),
            CancellationToken.None);

        var dollars = await CreateHandler(DollarTenant, currency, repository).HandleAsync(
            new CreateProductCommand("Trucker Cap", "CAP-001", null, 24.50m, null, null),
            CancellationToken.None);

        Assert.Equal("GBP", pounds.Value.Price.CurrencyCode);
        Assert.Equal("USD", dollars.Value.Price.CurrencyCode);

        // The crossing case: the GBP tenant asks for dollars, and the USD tenant asks for pounds. Both are refused,
        // so a price can never be quoted in a currency its own shop does not use.
        var dollarsForThePoundShop = await CreateHandler(PoundTenant, currency, repository).HandleAsync(
            new CreateProductCommand("Wool Scarf", "SCARF-001", null, 12.00m, "USD", null),
            CancellationToken.None);

        var poundsForTheDollarShop = await CreateHandler(DollarTenant, currency, repository).HandleAsync(
            new CreateProductCommand("Sun Hat", "HAT-001", null, 12.00m, "GBP", null),
            CancellationToken.None);

        Assert.Equal("Product.CurrencyMismatch", dollarsForThePoundShop.Error.Code);
        Assert.Equal("Product.CurrencyMismatch", poundsForTheDollarShop.Error.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, dollarsForThePoundShop.Error.ToProblem() is Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult problem
            ? problem.StatusCode
            : 0);
    }

    [Fact]
    public async Task A_request_naming_a_currency_that_is_not_the_tenants_is_a_400_and_adds_nothing()
    {
        var repository = new FakeProductRepository();

        var result = await CreateHandler(PoundTenant, Currency().With(PoundTenant, "GBP"), repository).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, "EUR", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.CurrencyMismatch", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Updating_a_product_denominates_the_new_price_in_the_currency_the_tenant_uses_now()
    {
        // The second documented behaviour of a currency change: a re-submitted product moves to the new currency,
        // because the price in the request is a new price, not a rediscovery of the old one.
        var product = NewProduct(EuroTenant, "TEE-001", "EUR");
        var repository = new FakeProductRepository(product);

        var result = await UpdateHandler(PoundTenant, Currency().With(PoundTenant, "GBP"), repository).HandleAsync(
            new UpdateProductCommand(product.Id, "Classic Tee", "TEE-001", null, 21.00m, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("GBP", result.Value.Price.CurrencyCode);
        Assert.Equal(21.00m, result.Value.Price.Amount);
    }

    // ---------------------------------------------------------------- no silent fallback

    [Fact]
    public async Task An_unreachable_registry_refuses_the_write_instead_of_guessing_a_currency()
    {
        // The decision this test exists for: there is no fallback. A refused write is visible immediately; a price
        // stamped with a guessed currency is discovered when someone is charged the wrong money.
        var repository = new FakeProductRepository();

        var result = await CreateHandler(PoundTenant, Currency().Unavailable(), repository).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TenantCurrency.Unavailable", result.Error.Code);
        Assert.Equal(ErrorType.Unexpected, result.Error.Type);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task A_tenant_the_registry_does_not_know_is_refused_as_a_404()
    {
        var repository = new FakeProductRepository();

        var result = await CreateHandler(
                PoundTenant,
                new FakeTenantCurrencyProvider(),
                repository)
            .HandleAsync(
                new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, null),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TenantCurrency.TenantNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task An_invalid_price_does_not_cost_a_lookup_in_the_registry()
    {
        // Ordering, pinned: a request that is already invalid must fail on its own field, not wait for — or be
        // attributed to — another service.
        var currency = Currency();

        var result = await CreateHandler(PoundTenant, currency).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.999m, null, null),
            CancellationToken.None);

        Assert.Equal("Product.InvalidPriceScale", result.Error.Code);
        Assert.Empty(currency.Requests);
    }

    [Fact]
    public async Task A_host_scope_write_is_refused_before_the_registry_is_asked()
    {
        var currency = Currency();

        var result = await CreateHandler(Guid.Empty, currency).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, null),
            CancellationToken.None);

        Assert.Equal("Product.TenantRequired", result.Error.Code);
        Assert.Empty(currency.Requests);
    }

    // ---------------------------------------------------------------- reading both currencies

    [Fact]
    public async Task A_detail_read_reports_the_stored_currency_and_the_currency_the_tenant_uses_now()
    {
        // The scenario a currency change creates: the row says EUR, the shop now prices in GBP. Both are reported,
        // and the stored one wins for the amount it belongs to.
        var product = NewProduct(PoundTenant, "TEE-001", "EUR");

        var result = await DetailHandler(PoundTenant, Currency().With(PoundTenant, "GBP"), product).HandleAsync(
            new GetProductQuery(product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value.Price.CurrencyCode);
        Assert.Equal("GBP", result.Value.TenantCurrency);
    }

    [Fact]
    public async Task A_detail_read_still_answers_when_the_registry_cannot_be_reached()
    {
        // A read stamps nothing, so degrading here is honest: the stored price is the fact, and the advisory field is
        // absent rather than wrong.
        var product = NewProduct(PoundTenant, "TEE-001", "GBP");

        var result = await DetailHandler(PoundTenant, Currency().Unavailable(), product).HandleAsync(
            new GetProductQuery(product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("GBP", result.Value.Price.CurrencyCode);
        Assert.Null(result.Value.TenantCurrency);
    }

    // ---------------------------------------------------------------- the tenant aggregate

    [Fact]
    public void A_new_tenant_prices_in_euros_by_default()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Acme");

        Assert.Equal(Tenant.DefaultCurrency, tenant.Currency);
        Assert.Equal("EUR", tenant.Currency);
    }

    [Fact]
    public void Changing_the_currency_normalizes_the_code_and_raises_an_event()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Acme");

        var changed = tenant.ChangeCurrency("gbp");

        Assert.True(changed);
        Assert.Equal("GBP", tenant.Currency);
        Assert.Contains(
            tenant.DomainEvents,
            domainEvent => domainEvent is TenantCurrencyChangedDomainEvent { PreviousCurrency: "EUR", Currency: "GBP" });
    }

    [Fact]
    public void Setting_the_currency_the_tenant_already_has_changes_nothing()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Acme");

        Assert.False(tenant.ChangeCurrency("eur"));
        Assert.Empty(tenant.DomainEvents.OfType<TenantCurrencyChangedDomainEvent>());
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("")]
    [InlineData(null)]
    public void A_currency_that_is_not_three_letters_is_rejected(string? currency)
    {
        var error = Tenant.ValidateCurrency(currency);

        Assert.NotNull(error);
        Assert.Equal("Tenant.InvalidCurrency", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        // The same rule, thrown, for a caller that skipped the check.
        Assert.Throws<ArgumentException>(() => Tenant.Create(Guid.NewGuid(), "Acme").ChangeCurrency(currency!));
    }

    // ---------------------------------------------------------------- the change use case

    [Fact]
    public async Task Changing_a_tenants_currency_returns_both_codes()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Acme");
        var handler = new ChangeTenantCurrencyCommandHandler(new FakeTenantRepository(tenant));

        var result = await handler.HandleAsync(
            new ChangeTenantCurrencyCommand(tenant.Id, "GBP"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value.PreviousCurrency);
        Assert.Equal("GBP", result.Value.Currency);
        Assert.True(result.Value.Changed);
    }

    [Fact]
    public async Task Changing_the_currency_of_a_missing_tenant_is_a_404()
    {
        var handler = new ChangeTenantCurrencyCommandHandler(new FakeTenantRepository());

        var result = await handler.HandleAsync(
            new ChangeTenantCurrencyCommand(Guid.NewGuid(), "GBP"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Changing_the_currency_to_a_non_code_is_a_400()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Acme");
        var handler = new ChangeTenantCurrencyCommandHandler(new FakeTenantRepository(tenant));

        var result = await handler.HandleAsync(
            new ChangeTenantCurrencyCommand(tenant.Id, "Kittens"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.InvalidCurrency", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("EUR", tenant.Currency);
    }

    // ---------------------------------------------------------------- the cache

    [Fact]
    public async Task A_resolved_currency_is_cached_so_a_burst_of_writes_costs_one_lookup()
    {
        var inner = Currency().With(PoundTenant, "GBP");
        var cache = new CachedTenantCurrencyProvider(inner, new MemoryCache(new MemoryCacheOptions()));

        var first = await cache.GetCurrencyAsync(PoundTenant);
        var second = await cache.GetCurrencyAsync(PoundTenant);

        Assert.Equal("GBP", first.Value);
        Assert.Equal("GBP", second.Value);
        Assert.Single(inner.Requests);
    }

    [Fact]
    public async Task A_failed_lookup_is_not_cached()
    {
        // The property that keeps an outage from turning into a fixed window of refused writes after it ends.
        var inner = Currency().With(PoundTenant, "GBP");
        var cache = new CachedTenantCurrencyProvider(inner, new MemoryCache(new MemoryCacheOptions()));

        inner.Failure = TenantCurrencyErrors.Unavailable(PoundTenant);
        var unavailable = await cache.GetCurrencyAsync(PoundTenant);

        inner.Failure = null;
        var recovered = await cache.GetCurrencyAsync(PoundTenant);

        Assert.True(unavailable.IsFailure);
        Assert.True(recovered.IsSuccess);
        Assert.Equal("GBP", recovered.Value);
        Assert.Equal(2, inner.Requests.Count);
    }

    [Fact]
    public async Task The_cache_is_keyed_by_tenant_so_one_tenants_currency_is_never_anothers()
    {
        var inner = Currency().With(PoundTenant, "GBP").With(DollarTenant, "USD");
        var cache = new CachedTenantCurrencyProvider(inner, new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal("GBP", (await cache.GetCurrencyAsync(PoundTenant)).Value);
        Assert.Equal("USD", (await cache.GetCurrencyAsync(DollarTenant)).Value);
    }

    // ---------------------------------------------------------------- the client's binding

    [Fact]
    public void The_production_wiring_resolves_a_tenant_currency_provider()
    {
        // The whole edge as the catalog registers it — the generated Refit client, the client-credentials handler,
        // the transport and the cache — resolved from the composition root's own method. Refit's generated client is
        // resolved by type, so a missing implementation is a runtime failure that only shows up in a running stack as
        // "the registry cannot be reached"; this catches it at test time instead.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalClients:Backend:ClientId"] = "shopit-backend",
                ["InternalClients:Backend:ClientSecret"] = "not-a-real-secret",
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddCatalogInfrastructure(configuration)
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ITenantCurrencyProvider>());
    }

    // ---------------------------------------------------------------- the internal policy

    [Fact]
    public async Task The_internal_policy_admits_the_backend_client_and_refuses_user_tokens()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAuthorization(options => options.AddInternalServicePolicy("shopit-backend"))
            .BuildServiceProvider();

        var authorization = provider.GetRequiredService<IAuthorizationService>();

        // A client-credentials token: the subject is the client id, not a GUID.
        var serviceToken = Principal("shopit-backend", authenticated: true);
        // An interactive user token: the subject is the user's id.
        var userToken = Principal(Guid.NewGuid().ToString(), authenticated: true);
        // Some other service's token, however well formed.
        var strangerToken = Principal("someone-else", authenticated: true);

        Assert.True((await authorization.AuthorizeAsync(serviceToken, null, InternalServicePolicy.Name)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(userToken, null, InternalServicePolicy.Name)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(strangerToken, null, InternalServicePolicy.Name)).Succeeded);
    }

    // ---------------------------------------------------------------- fixtures

    private static FakeTenantCurrencyProvider Currency() => new FakeTenantCurrencyProvider().With(EuroTenant, "EUR");

    private static CreateProductCommandHandler CreateHandler(
        Guid tenantId,
        FakeTenantCurrencyProvider currency,
        FakeProductRepository? repository = null) =>
        new(
            repository ?? new FakeProductRepository(),
            new FakeCategoryRepository(),
            new FakeCurrentTenant(tenantId),
            currency);

    private static UpdateProductCommandHandler UpdateHandler(
        Guid tenantId,
        FakeTenantCurrencyProvider currency,
        FakeProductRepository repository) =>
        new(repository, new FakeCategoryRepository(), new FakeCurrentTenant(tenantId), currency);

    private static GetProductQueryHandler DetailHandler(
        Guid tenantId,
        FakeTenantCurrencyProvider currency,
        Product product) =>
        new(
            new FakeProductRepository(product),
            new FakeCurrentTenant(tenantId),
            currency,
            NullLogger<GetProductQueryHandler>.Instance);

    private static Product NewProduct(Guid tenantId, string sku, string currency) =>
        Product.Create(
            Guid.NewGuid(),
            tenantId,
            "Classic Tee",
            sku,
            null,
            Money.Create(19.99m, currency),
            null);

    private static ClaimsPrincipal Principal(string subject, bool authenticated) =>
        new(new ClaimsIdentity([new Claim("sub", subject)], authenticated ? "Bearer" : null));

    private sealed class FakeCurrentTenant(Guid id) : ICurrentTenant
    {
        public Guid Id { get; } = id;

        public string? Name => Id == Guid.Empty ? "Host" : "Test Tenant";

        public IDisposable Change(TenantInfo tenant) =>
            throw new InvalidOperationException("These use cases never change tenant scope.");
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<Category?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Category?>(null);

        public Task<Category> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not used.");

        public Task AddAsync(Category entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(Category entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveAsync(Category entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<(IEnumerable<Category> Items, long TotalCount, long TotalPages)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IEnumerable<Category>, long, long)>(([], 0, 0));
    }

    private sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        private readonly List<Product> _products = [.. products];

        public List<Product> Added { get; } = [];

        public Task<Product?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.FirstOrDefault(product => product.Id == id));

        public Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The handlers must use FindByIdAsync.");

        public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            _products.Add(entity);
            Added.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveAsync(Product entity, CancellationToken cancellationToken = default)
        {
            _products.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            var normalizedSku = Product.NormalizeSku(sku);

            return Task.FromResult(_products.Any(product => product.NormalizedSku == normalizedSku));
        }

        public Task<bool> AnyByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.Any(product => product.CategoryId == categoryId));

        public Task<(IEnumerable<Product> Items, long TotalCount, long TotalPages)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? filter,
            Guid? categoryId,
            ProductStatus? status,
            ProductSortField sortBy,
            ProductSortOrder sortOrder,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IEnumerable<Product>, long, long)>((_products, _products.Count, 1));
    }

    private sealed class FakeTenantRepository(params Tenant[] tenants) : ITenantRepository
    {
        private readonly List<Tenant> _tenants = [.. tenants];

        public Task<Tenant?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_tenants.FirstOrDefault(tenant => tenant.Id == id));

        public Task<Tenant> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The handlers must use FindByIdAsync.");

        public Task AddAsync(Tenant entity, CancellationToken cancellationToken = default)
        {
            _tenants.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Tenant entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveAsync(Tenant entity, CancellationToken cancellationToken = default)
        {
            _tenants.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(_tenants.Any(tenant => string.Equals(tenant.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<(IEnumerable<Tenant> Items, long TotalCount, long TotalPages)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IEnumerable<Tenant>, long, long)>((_tenants, _tenants.Count, 1));
    }
}
