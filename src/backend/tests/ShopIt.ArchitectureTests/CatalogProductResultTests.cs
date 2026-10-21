using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using ShopIt.Catalog.Application;
using ShopIt.Catalog.Application.Categories.Commands.DeleteCategory;
using ShopIt.Catalog.Application.Products.Commands.ChangeProductStatus;
using ShopIt.Catalog.Application.Products.Commands.CreateProduct;
using ShopIt.Catalog.Application.Products.Commands.UpdateProduct;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Events;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Presentation.Results;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers the product aggregate and use cases of #78 that can be checked without a database: the money rules
/// reaching the command layer, per-tenant SKU uniqueness, the category reference, the lifecycle, and the
/// error-to-status contract the presentation layer depends on.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Scope, stated honestly.</strong> These fakes do not reproduce tenant isolation — the query filter
/// is EF Core's, and a fake repository that filtered by tenant would be testing the fake. The cross-tenant
/// guarantees (tenant A cannot see tenant B's products, the same SKU exists in two tenants) belong to #80's
/// proof against a real database.
/// </para>
/// <para>
/// What is pinned here is that each expected outcome is a <c>Result</c> failure carrying the right
/// <see cref="ErrorType"/>, that <c>ToProblem()</c> turns that type into the documented status code, and that
/// the aggregate — not the caller — decides the lifecycle and the price.
/// </para>
/// </remarks>
public class CatalogProductResultTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Theory]
    [InlineData(nameof(ProductErrors.NotFound), StatusCodes.Status404NotFound)]
    [InlineData(nameof(ProductErrors.SkuAlreadyExists), StatusCodes.Status409Conflict)]
    [InlineData(nameof(ProductErrors.CategoryNotFound), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.ArchivedIsImmutable), StatusCodes.Status409Conflict)]
    [InlineData(nameof(ProductErrors.InvalidStatusTransition), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.NameRequired), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.NameTooLong), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.SkuRequired), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.SkuTooLong), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.SkuInvalidCharacters), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.DescriptionTooLong), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.NegativePrice), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.InvalidPriceScale), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.InvalidCurrency), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.CurrencyMismatch), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.PriceRequired), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.StatusRequired), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(ProductErrors.TenantRequired), StatusCodes.Status403Forbidden)]
    public void Each_product_error_maps_to_the_documented_status_code(string error, int expectedStatus)
    {
        var problem = Assert.IsType<ProblemHttpResult>(ErrorNamed(error).ToProblem());

        Assert.Equal(expectedStatus, problem.StatusCode);
    }

    [Fact]
    public async Task Creating_a_product_stores_the_acting_tenant_the_default_draft_status_and_the_catalog_currency()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", "Cotton.", 19.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, result.Value.TenantId);
        Assert.Equal(ProductStatus.Draft, result.Value.Status);
        Assert.Equal("TEE-001", result.Value.Sku);

        // The currency was omitted by the caller and stamped from the catalog's setting (#84's interim).
        Assert.Equal(19.99m, result.Value.Price.Amount);
        Assert.Equal("EUR", result.Value.Price.CurrencyCode);

        Assert.NotEqual(default, result.Value.CreatedOn);
    }

    [Fact]
    public async Task A_request_naming_another_currency_is_a_Validation_failure()
    {
        // Silently re-stamping 19.99 USD as 19.99 EUR would be a financial error, so it is refused.
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, "USD", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.CurrencyMismatch", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task The_currency_comparison_ignores_casing_because_a_code_is_a_code()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, "eur", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value.Price.CurrencyCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(19.999)]
    public async Task An_invalid_price_is_a_field_level_Validation_failure(decimal amount)
    {
        var repository = new FakeProductRepository();

        var result = await CreateHandler(repository).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, amount, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task A_duplicate_sku_in_the_tenant_is_a_Conflict_failure_regardless_of_casing()
    {
        var existing = NewProduct(sku: "TEE-001");

        var result = await CreateHandler(new FakeProductRepository(existing)).HandleAsync(
            new CreateProductCommand("Another tee", "tee-001", null, 9.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Product.SkuAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task A_category_that_does_not_exist_in_this_tenant_is_a_Validation_failure_and_is_never_assigned()
    {
        // No foreign category id: another tenant's id resolves to null under the tenant filter, exactly like
        // an id that never existed, so there is nothing to assign.
        var productRepository = new FakeProductRepository();

        var result = await CreateHandler(productRepository).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.CategoryNotFound", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(productRepository.Added);
    }

    [Fact]
    public async Task An_empty_category_id_is_reported_as_a_malformed_field_not_a_missing_category()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.CategoryIdEmpty", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Creating_a_product_in_the_tenant_it_was_filed_under_keeps_the_category()
    {
        var category = Category.Create(Guid.NewGuid(), TenantId, "Apparel");

        var result = await CreateHandler(categories: [category]).HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, category.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(category.Id, result.Value.CategoryId);
    }

    [Fact]
    public async Task A_host_scope_write_is_refused_with_a_Result_failure()
    {
        // No tenant claim means host scope, where the query filter is off and a new row would have no owner.
        var handler = CreateHandler(currentTenantId: Guid.Empty);

        var result = await handler.HandleAsync(
            new CreateProductCommand("Classic Tee", "TEE-001", null, 19.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Product.TenantRequired", result.Error.Code);
    }

    [Fact]
    public async Task Updating_a_missing_product_is_NotFound()
    {
        var result = await UpdateHandler().HandleAsync(
            new UpdateProductCommand(Guid.NewGuid(), "Classic Tee", "TEE-001", null, 19.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task An_archived_product_cannot_be_updated()
    {
        var product = NewProduct(sku: "TEE-001");
        product.ChangeStatus(ProductStatus.Active);
        product.ChangeStatus(ProductStatus.Archived);

        var result = await UpdateHandler(new FakeProductRepository(product)).HandleAsync(
            new UpdateProductCommand(product.Id, "Renamed", "TEE-001", null, 19.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Product.ArchivedIsImmutable", result.Error.Code);
    }

    [Fact]
    public async Task Updating_a_product_to_its_own_sku_in_another_case_is_not_a_conflict()
    {
        var product = NewProduct(sku: "TEE-001");

        var result = await UpdateHandler(new FakeProductRepository(product)).HandleAsync(
            new UpdateProductCommand(product.Id, "Classic Tee", "tee-001", null, 19.99m, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        // The client's spelling is stored; the uniqueness key is still the uppercase form, so the next create
        // with "TEE-001" is still a conflict.
        Assert.Equal("tee-001", result.Value.Sku);
        Assert.Equal("TEE-001", product.NormalizedSku);
    }

    [Fact]
    public async Task Activating_a_draft_product_changes_the_status_and_raises_the_event()
    {
        var product = NewProduct();
        var handler = new ChangeProductStatusCommandHandler(
            new FakeProductRepository(product),
            new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(
            new ChangeProductStatusCommand(product.Id, ProductStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductStatus.Draft, result.Value.PreviousStatus);
        Assert.Equal(ProductStatus.Active, result.Value.Status);
        Assert.True(result.Value.Changed);
        Assert.Equal(ProductStatus.Active, product.Status);
        Assert.Contains(product.DomainEvents, domainEvent => domainEvent is ProductStatusChangedDomainEvent);
    }

    [Fact]
    public async Task Asking_for_the_status_a_product_already_has_is_a_successful_no_op()
    {
        var product = NewProduct();
        var repository = new FakeProductRepository(product);
        var handler = new ChangeProductStatusCommandHandler(repository, new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(
            new ChangeProductStatusCommand(product.Id, ProductStatus.Draft),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Changed);

        // No write: a request that changed nothing must not stamp the audit trail as if it had.
        Assert.Equal(0, repository.UpdateCount);
    }

    [Theory]
    [InlineData(ProductStatus.Active, ProductStatus.Draft)]
    [InlineData(ProductStatus.Active, ProductStatus.Active)]
    [InlineData(ProductStatus.Archived, ProductStatus.Active)]
    [InlineData(ProductStatus.Archived, ProductStatus.Draft)]
    public async Task A_transition_the_lifecycle_does_not_allow_is_a_Validation_failure(
        ProductStatus from,
        ProductStatus to)
    {
        var product = NewProduct();
        product.ChangeStatus(from);

        var result = await new ChangeProductStatusCommandHandler(
                new FakeProductRepository(product),
                new FakeCurrentTenant(TenantId))
            .HandleAsync(new ChangeProductStatusCommand(product.Id, to), CancellationToken.None);

        // Coming back to the status it already has is allowed and idempotent; every other pair here is not.
        if (from == to)
        {
            Assert.True(result.IsSuccess);
            Assert.False(result.Value.Changed);
            return;
        }

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidStatusTransition", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Archiving_is_terminal_so_the_aggregate_refuses_to_restore()
    {
        // The handler returns the Result failure; the aggregate throws, because reaching it means a bug.
        var product = NewProduct();
        product.ChangeStatus(ProductStatus.Active);
        product.ChangeStatus(ProductStatus.Archived);

        Assert.Throws<InvalidOperationException>(() => product.ChangeStatus(ProductStatus.Active));
    }

    [Fact]
    public void The_status_cannot_be_written_by_a_public_setter()
    {
        // "Transitions are explicit commands, not a free-form field write" is only true while the aggregate's
        // own state has no public setter — otherwise a handler could bypass the lifecycle, and this test
        // exists so adding one is a deliberate, visible change. The framework's audit fields are declared on
        // AuditedAggregateRoot and are the persistence convention's to write (ADR-0004), so they are out of
        // scope here rather than exempted by name.
        var settable = typeof(Product)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.DeclaringType == typeof(Product))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToList();

        Assert.Empty(settable);
    }

    [Fact]
    public async Task Deleting_a_category_that_still_has_products_is_a_Conflict_failure()
    {
        var category = Category.Create(Guid.NewGuid(), TenantId, "Apparel");
        var product = NewProduct(categoryId: category.Id);

        var categoryRepository = new FakeCategoryRepository(category);

        var result = await new DeleteCategoryCommandHandler(
                categoryRepository,
                new FakeProductRepository(product),
                new FakeCurrentTenant(TenantId))
            .HandleAsync(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Category.CategoryHasProducts", result.Error.Code);
        Assert.Contains(category, categoryRepository.Items);
    }

    [Fact]
    public async Task Deleting_a_category_no_product_references_succeeds()
    {
        var category = Category.Create(Guid.NewGuid(), TenantId, "Apparel");
        var categoryRepository = new FakeCategoryRepository(category);

        var result = await new DeleteCategoryCommandHandler(
                categoryRepository,
                new FakeProductRepository(),
                new FakeCurrentTenant(TenantId))
            .HandleAsync(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(category, categoryRepository.Items);
    }

    [Fact]
    public async Task Deleting_a_missing_category_is_NotFound()
    {
        var result = await new DeleteCategoryCommandHandler(
                new FakeCategoryRepository(),
                new FakeProductRepository(),
                new FakeCurrentTenant(TenantId))
            .HandleAsync(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public void A_status_serialises_as_its_name_not_its_number()
    {
        // The HTTP contract, not an implementation detail: a client reads "Active", and renumbering the enum
        // cannot change what a stored response meant.
        Assert.Equal("\"Active\"", JsonSerializer.Serialize(ProductStatus.Active));
        Assert.Equal(ProductStatus.Archived, JsonSerializer.Deserialize<ProductStatus>("\"Archived\""));
    }

    private static CreateProductCommandHandler CreateHandler(
        FakeProductRepository? products = null,
        Category[]? categories = null,
        Guid? currentTenantId = null) =>
        new(
            products ?? new FakeProductRepository(),
            new FakeCategoryRepository(categories ?? []),
            new FakeCurrentTenant(currentTenantId ?? TenantId),
            Options.Create(new CatalogOptions { Currency = "EUR" }));

    private static UpdateProductCommandHandler UpdateHandler(
        FakeProductRepository? products = null,
        Category[]? categories = null) =>
        new(
            products ?? new FakeProductRepository(),
            new FakeCategoryRepository(categories ?? []),
            new FakeCurrentTenant(TenantId),
            Options.Create(new CatalogOptions { Currency = "EUR" }));

    private static Product NewProduct(string sku = "TEE-001", Guid? categoryId = null) =>
        Product.Create(
            Guid.NewGuid(),
            TenantId,
            "Classic Tee",
            sku,
            "Cotton.",
            Money.Create(19.99m, "EUR"),
            categoryId);

    private static Error ErrorNamed(string factory)
    {
        var id = Guid.NewGuid();

        return factory switch
        {
            nameof(ProductErrors.NotFound) => ProductErrors.NotFound(id),
            nameof(ProductErrors.SkuAlreadyExists) => ProductErrors.SkuAlreadyExists("TEE-001"),
            nameof(ProductErrors.CategoryNotFound) => ProductErrors.CategoryNotFound(id),
            nameof(ProductErrors.ArchivedIsImmutable) => ProductErrors.ArchivedIsImmutable(id),
            nameof(ProductErrors.InvalidStatusTransition) =>
                ProductErrors.InvalidStatusTransition(ProductStatus.Active, ProductStatus.Draft),
            nameof(ProductErrors.NameRequired) => ProductErrors.NameRequired(),
            nameof(ProductErrors.NameTooLong) => ProductErrors.NameTooLong(Product.MaxNameLength),
            nameof(ProductErrors.SkuRequired) => ProductErrors.SkuRequired(),
            nameof(ProductErrors.SkuTooLong) => ProductErrors.SkuTooLong(Product.MaxSkuLength),
            nameof(ProductErrors.SkuInvalidCharacters) => ProductErrors.SkuInvalidCharacters("TEE 001"),
            nameof(ProductErrors.DescriptionTooLong) =>
                ProductErrors.DescriptionTooLong(Product.MaxDescriptionLength),
            nameof(ProductErrors.NegativePrice) => ProductErrors.NegativePrice(),
            nameof(ProductErrors.InvalidPriceScale) => ProductErrors.InvalidPriceScale(Money.Scale),
            nameof(ProductErrors.InvalidCurrency) => ProductErrors.InvalidCurrency("EURO"),
            nameof(ProductErrors.CurrencyMismatch) => ProductErrors.CurrencyMismatch("EUR", "USD"),
            nameof(ProductErrors.PriceRequired) => ProductErrors.PriceRequired(),
            nameof(ProductErrors.StatusRequired) => ProductErrors.StatusRequired(),
            nameof(ProductErrors.TenantRequired) => ProductErrors.TenantRequired(),
            _ => throw new ArgumentOutOfRangeException(nameof(factory), factory, "Unknown product error."),
        };
    }

    /// <summary>An in-memory stand-in: enough to drive the handlers, and deliberately tenant-blind.</summary>
    private sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        private readonly List<Product> _products = [.. products];

        public List<Product> Added { get; } = [];

        public int UpdateCount { get; private set; }

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

        public Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

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
    }

    private sealed class FakeCategoryRepository(params Category[] categories) : ICategoryRepository
    {
        private readonly List<Category> _categories = [.. categories];

        public IReadOnlyList<Category> Items => _categories;

        public Task<Category?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.FirstOrDefault(category => category.Id == id));

        public Task<Category> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The handlers must use FindByIdAsync.");

        public Task AddAsync(Category entity, CancellationToken cancellationToken = default)
        {
            _categories.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Category entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveAsync(Category entity, CancellationToken cancellationToken = default)
        {
            _categories.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalizedName = Category.NormalizeName(name);

            return Task.FromResult(_categories.Any(category => category.NormalizedName == normalizedName));
        }

        public Task<(IEnumerable<Category> Items, long TotalCount, long TotalPages)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? filter,
            CancellationToken cancellationToken = default)
        {
            var items = _categories
                .Where(category => string.IsNullOrWhiteSpace(filter)
                                   || category.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(category => category.Name)
                .ThenBy(category => category.Id)
                .ToList();

            var totalCount = items.Count;
            var totalPages = (totalCount / pageSize) + (totalCount % pageSize > 0 ? 1 : 0);

            return Task.FromResult<(IEnumerable<Category>, long, long)>((
                items.Skip((pageNumber - 1) * pageSize).Take(pageSize),
                totalCount,
                totalPages));
        }
    }

    private sealed class FakeCurrentTenant(Guid id) : ICurrentTenant
    {
        public Guid Id { get; } = id;

        public string? Name => Id == Guid.Empty ? "Host" : "Test Tenant";

        public IDisposable Change(TenantInfo tenant) =>
            throw new InvalidOperationException("These use cases never change tenant scope.");
    }
}
