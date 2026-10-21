using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ShopIt.Catalog.Application.Products.Queries.GetProduct;
using ShopIt.Catalog.Application.Products.Queries.GetProducts;
using ShopIt.Catalog.Domain.Products;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Tenancy;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers the catalog read surface of #79 that can be checked without a database: paging and sort validation
/// failing as <c>Result</c>s (a 400), omission meaning the documented default (not a silent fallback for a
/// typo), tenant scope, the detail read's "absent category is a normal state" rule, and ADR-0005's JSON
/// contract for a price.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Scope, stated honestly.</strong> The SQL translation is not under test here: <c>ILIKE</c> on
/// name-or-SKU, the equality filters and the <c>Id</c> tie-breaker are EF Core's, and a fake that reimplemented
/// them would be testing the fake. What is pinned is that the handler forwards each filter and the resolved
/// sort to the repository, that every invalid input is a <see cref="ErrorType.Validation"/> failure with the
/// documented code, and that the DTOs serialise money as a string. The database-backed guarantees belong to the
/// live verification recorded on the pull request.
/// </para>
/// <para>
/// The fake repository is deliberately a <em>recorder</em>: it returns what it was constructed with and keeps
/// the arguments it was called with, so a test can tell whether the handler passed a filter through unchanged.
/// </para>
/// </remarks>
public class CatalogProductQueryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    /// <summary>
    /// The serializer settings ASP.NET Core's minimal APIs use for JSON: web defaults (camelCase, case-insensitive
    /// reads). Pinning the DTO shape against these is what makes the assertion about the real HTTP contract
    /// rather than about a test-only configuration.
    /// </summary>
    private static readonly JsonSerializerOptions HttpJsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(0, 20, "Product.InvalidPageNumber")]
    [InlineData(-1, 20, "Product.InvalidPageNumber")]
    [InlineData(1, 0, "Product.InvalidPageSize")]
    [InlineData(1, 101, "Product.InvalidPageSize")]
    public async Task Paging_outside_the_documented_bounds_is_a_Validation_failure(
        int page,
        int pageSize,
        string expectedCode)
    {
        var repository = new RecordingProductRepository();

        var result = await ListHandler(repository).HandleAsync(
            Query(page: page, pageSize: pageSize),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(expectedCode, result.Error.Code);

        // Not clamped: the page size the client asked for was never turned into a different query.
        Assert.Null(repository.LastListCall);
    }

    [Theory]
    [InlineData("colour")]
    [InlineData("name desc")]
    [InlineData("created")]
    public async Task An_unknown_sort_key_is_a_Validation_failure_not_a_silent_default(string sortBy)
    {
        // "created" is the near-miss a client is most likely to type; the API refuses it and names the keys it
        // does support instead of quietly returning the name order.
        var repository = new RecordingProductRepository();

        var result = await ListHandler(repository).HandleAsync(
            Query(sortBy: sortBy),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidSortBy", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Null(repository.LastListCall);
    }

    [Theory]
    [InlineData("sideways")]
    [InlineData("ascending")]
    public async Task An_unknown_sort_direction_is_a_Validation_failure(string sortOrder)
    {
        var repository = new RecordingProductRepository();

        var result = await ListHandler(repository).HandleAsync(
            Query(sortOrder: sortOrder),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.InvalidSortOrder", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Null(repository.LastListCall);
    }

    [Fact]
    public async Task Sort_keys_and_directions_are_matched_case_insensitively()
    {
        var repository = new RecordingProductRepository();

        var result = await ListHandler(repository).HandleAsync(
            Query(sortBy: "PRICE", sortOrder: "DESC"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastListCall);
        Assert.Equal(ProductSortField.Price, repository.LastListCall.SortBy);
        Assert.Equal(ProductSortOrder.Descending, repository.LastListCall.SortOrder);
    }

    [Fact]
    public async Task An_omitted_sort_uses_name_ascending()
    {
        var repository = new RecordingProductRepository();

        var result = await ListHandler(repository).HandleAsync(
            Query(sortBy: null, sortOrder: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastListCall);
        Assert.Equal(ProductSortField.Name, repository.LastListCall.SortBy);
        Assert.Equal(ProductSortOrder.Ascending, repository.LastListCall.SortOrder);
    }

    [Fact]
    public async Task A_valid_page_forwards_every_filter_and_returns_the_items_and_counts()
    {
        var categoryId = Guid.NewGuid();
        var matching = NewProduct(name: "Classic Tee", sku: "TEE-001", categoryId: categoryId);
        var repository = new RecordingProductRepository(matching);

        var result = await ListHandler(repository).HandleAsync(
            Query(
                page: 1,
                pageSize: 5,
                filter: "tee",
                categoryId: categoryId,
                status: ProductStatus.Active,
                sortBy: "sku",
                sortOrder: "desc"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var call = Assert.IsType<RecordingProductRepository.ListCall>(repository.LastListCall);
        Assert.Equal(1, call.PageNumber);
        Assert.Equal(5, call.PageSize);
        Assert.Equal("tee", call.Filter);
        Assert.Equal(categoryId, call.CategoryId);
        Assert.Equal(ProductStatus.Active, call.Status);
        Assert.Equal(ProductSortField.Sku, call.SortBy);
        Assert.Equal(ProductSortOrder.Descending, call.SortOrder);

        // Counts come from the repository (they are the count before paging), not from the page's length.
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(5, result.Value.PageSize);
        Assert.Single(result.Value.Items);
        Assert.Equal("TEE-001", result.Value.Items.Single().Sku);
        Assert.Equal(19.99m, result.Value.Items.Single().Price.Amount);
    }

    [Fact]
    public async Task A_host_scope_list_is_refused_with_a_Result_failure()
    {
        // Host scope disables the tenant filter, so an answered list would be every tenant's catalog.
        var result = await new GetProductsQueryHandler(
                new RecordingProductRepository(),
                new FakeCurrentTenant(Guid.Empty))
            .HandleAsync(Query(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Product.TenantRequired", result.Error.Code);
    }

    [Fact]
    public async Task Reading_a_product_reports_an_absent_category_as_null_rather_than_failing()
    {
        var product = NewProduct();

        var result = await DetailHandler(new RecordingProductRepository(product)).HandleAsync(
            new GetProductQuery(product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CategoryId);
        Assert.Equal(product.Id, result.Value.Id);
        Assert.Equal(TenantId, result.Value.TenantId);
        Assert.Equal(19.99m, result.Value.Price.Amount);
        Assert.Equal("EUR", result.Value.Price.CurrencyCode);
    }

    [Fact]
    public async Task Reading_an_id_the_tenant_filter_hides_is_the_domain_NotFound_error()
    {
        // The repository returns null for another tenant's id — that is what the query filter does — and the
        // handler must turn it into Product.NotFound, a 404, rather than a successful null.
        var anotherTenantsProductId = Guid.NewGuid();

        var result = await DetailHandler(new RecordingProductRepository()).HandleAsync(
            new GetProductQuery(anotherTenantsProductId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task A_host_scope_detail_read_is_refused_with_a_Result_failure()
    {
        var product = NewProduct();

        var result = await new GetProductQueryHandler(
                new RecordingProductRepository(product),
                new FakeCurrentTenant(Guid.Empty))
            .HandleAsync(new GetProductQuery(product.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public void A_product_response_serialises_the_price_as_a_string_amount_with_its_currency()
    {
        var product = NewProduct();
        var result = new GetProductResult(
            Id: product.Id,
            Name: product.Name,
            Sku: product.Sku,
            Description: product.Description,
            Price: product.Price,
            CategoryId: null,
            Status: ProductStatus.Active,
            TenantId: TenantId,
            CreatedOn: DateTimeOffset.UnixEpoch,
            LastModifiedOn: null);

        var json = JsonSerializer.Serialize(result, HttpJsonOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var price = root.GetProperty("price");

        // The amount is a JSON *string*, per ADR-0005: a number here would already have been through a double.
        Assert.Equal(JsonValueKind.String, price.GetProperty("amount").ValueKind);
        Assert.Equal("19.99", price.GetProperty("amount").GetString());
        Assert.Equal("EUR", price.GetProperty("currency").GetString());

        // The status is its name, and an absent category is a present null rather than a missing field.
        Assert.Equal("Active", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("categoryId").ValueKind);
    }

    [Fact]
    public void A_list_response_serialises_every_item_price_as_a_string_amount()
    {
        var product = NewProduct();
        var result = new GetProductsResult(
            Items:
            [
                new GetProductsProductItem(
                    product.Id,
                    product.Name,
                    product.Sku,
                    product.Description,
                    product.Price,
                    null,
                    ProductStatus.Active,
                    DateTimeOffset.UnixEpoch,
                    null)
            ],
            PageNumber: 1,
            PageSize: 20,
            TotalCount: 1,
            TotalPages: 1);

        var json = JsonSerializer.Serialize(result, HttpJsonOptions);

        using var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("items")[0];
        var amount = item.GetProperty("price").GetProperty("amount");

        Assert.Equal(JsonValueKind.String, amount.ValueKind);
        Assert.Equal("19.99", amount.GetString());
        Assert.Equal(1, document.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("totalPages").GetInt32());
    }

    private static GetProductsQueryHandler ListHandler(RecordingProductRepository? repository = null) =>
        new(repository ?? new RecordingProductRepository(), new FakeCurrentTenant(TenantId));

    private static GetProductQueryHandler DetailHandler(RecordingProductRepository? repository = null) =>
        new(repository ?? new RecordingProductRepository(), new FakeCurrentTenant(TenantId));

    private static GetProductsQuery Query(
        int page = 1,
        int pageSize = 20,
        string? filter = null,
        Guid? categoryId = null,
        ProductStatus? status = null,
        string? sortBy = null,
        string? sortOrder = null) =>
        new(page, pageSize, filter, categoryId, status, sortBy, sortOrder);

    private static Product NewProduct(
        string name = "Classic Tee",
        string sku = "TEE-001",
        Guid? categoryId = null)
    {
        var product = Product.Create(
            Guid.NewGuid(),
            TenantId,
            name,
            sku,
            "Cotton.",
            Money.Create(19.99m, "EUR"),
            categoryId);

        product.ChangeStatus(ProductStatus.Active);

        return product;
    }

    /// <summary>
    /// An in-memory stand-in that records the list arguments and serves a fixed set of products.
    /// </summary>
    /// <remarks>
    /// It does not filter or sort: those are the repository's SQL, and reimplementing them here would test this
    /// class instead. Its job is to let a test see exactly what the handler asked for.
    /// </remarks>
    private sealed class RecordingProductRepository(params Product[] products) : IProductRepository
    {
        private readonly List<Product> _products = [.. products];

        public ListCall? LastListCall { get; private set; }

        public Task<(IEnumerable<Product> Items, long TotalCount, long TotalPages)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? filter,
            Guid? categoryId,
            ProductStatus? status,
            ProductSortField sortBy,
            ProductSortOrder sortOrder,
            CancellationToken cancellationToken = default)
        {
            LastListCall = new ListCall(pageNumber, pageSize, filter, categoryId, status, sortBy, sortOrder);

            var totalCount = _products.Count;
            var totalPages = (totalCount / pageSize) + (totalCount % pageSize > 0 ? 1 : 0);

            return Task.FromResult<(IEnumerable<Product>, long, long)>((
                _products.Skip((pageNumber - 1) * pageSize).Take(pageSize),
                totalCount,
                totalPages));
        }

        public Task<Product?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.FirstOrDefault(product => product.Id == id));

        public Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The handlers must use FindByIdAsync.");

        public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            _products.Add(entity);
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

        public sealed record ListCall(
            int PageNumber,
            int PageSize,
            string? Filter,
            Guid? CategoryId,
            ProductStatus? Status,
            ProductSortField SortBy,
            ProductSortOrder SortOrder);
    }

    private sealed class FakeCurrentTenant(Guid id) : ICurrentTenant
    {
        public Guid Id { get; } = id;

        public string? Name => Id == Guid.Empty ? "Host" : "Test Tenant";

        public IDisposable Change(TenantInfo tenant) =>
            throw new InvalidOperationException("These use cases never change tenant scope.");
    }
}
