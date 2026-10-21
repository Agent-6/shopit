using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ShopIt.Catalog.Application.Categories.Commands.CreateCategory;
using ShopIt.Catalog.Application.Categories.Commands.RenameCategory;
using ShopIt.Catalog.Application.Categories.Queries.GetCategories;
using ShopIt.Catalog.Application.Categories.Queries.GetCategory;
using ShopIt.Catalog.Domain.Categories;
using ShopIt.Catalog.Domain.Events;
using ShopIt.Catalog.Domain.Repositories;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Framework.Presentation.Results;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers the category aggregate and use cases of #77 that can be checked without a database: name
/// validity, per-tenant conflicts, tenant scope, paging bounds, and the error-to-status contract the
/// presentation layer depends on.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Scope, stated honestly.</strong> These fakes do not reproduce tenant isolation — the query
/// filter is EF Core's, and a fake repository that filters by tenant would be testing the fake. The
/// cross-tenant guarantees (tenant A cannot see tenant B's rows; another tenant's id is a 404) belong to
/// #80's proof against a real database and are not duplicated here.
/// </para>
/// <para>
/// What is pinned here is that each expected outcome is a <c>Result</c> failure carrying the right
/// <see cref="ErrorType"/>, and that <c>ToProblem()</c> turns that type into the documented status code —
/// the AC's 404 / 409 / 403 / 400 answers.
/// </para>
/// </remarks>
public class CatalogCategoryResultTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Theory]
    [InlineData(nameof(CategoryErrors.NotFound), StatusCodes.Status404NotFound)]
    [InlineData(nameof(CategoryErrors.NameAlreadyExists), StatusCodes.Status409Conflict)]
    [InlineData(nameof(CategoryErrors.NameRequired), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CategoryErrors.NameTooLong), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CategoryErrors.InvalidPageNumber), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CategoryErrors.InvalidPageSize), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(CategoryErrors.TenantRequired), StatusCodes.Status403Forbidden)]
    [InlineData(nameof(CategoryErrors.CategoryHasProducts), StatusCodes.Status409Conflict)]
    public void Each_category_error_maps_to_the_documented_status_code(string error, int expectedStatus)
    {
        var problem = Assert.IsType<ProblemHttpResult>(ErrorNamed(error).ToProblem());

        Assert.Equal(expectedStatus, problem.StatusCode);
    }

    [Fact]
    public async Task Creating_a_category_stores_the_acting_tenant_and_the_trimmed_name()
    {
        var handler = new CreateCategoryCommandHandler(new FakeCategoryRepository(), new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(new CreateCategoryCommand("  Tools  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tools", result.Value.Name);
        Assert.Equal(TenantId, result.Value.TenantId);
        Assert.NotEqual(default, result.Value.CreatedOn);
    }

    [Fact]
    public async Task A_duplicate_name_in_the_same_tenant_is_a_Conflict_failure()
    {
        var existing = Category.Create(Guid.NewGuid(), TenantId, "Tools");
        var handler = new CreateCategoryCommandHandler(
            new FakeCategoryRepository(existing),
            new FakeCurrentTenant(TenantId));

        // Different case, same name: the uniqueness rule is case-insensitive, so this must not create a
        // second "tools" shelf next to "Tools".
        var result = await handler.HandleAsync(new CreateCategoryCommand("tools"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Category.NameAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task An_empty_name_is_a_Validation_failure_rather_than_an_exception()
    {
        var handler = new CreateCategoryCommandHandler(new FakeCategoryRepository(), new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(new CreateCategoryCommand("   "), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Category.NameRequired", result.Error.Code);
    }

    [Fact]
    public async Task A_name_longer_than_the_stored_maximum_is_a_Validation_failure()
    {
        var handler = new CreateCategoryCommandHandler(new FakeCategoryRepository(), new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(
            new CreateCategoryCommand(new string('a', Category.MaxNameLength + 1)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NameTooLong", result.Error.Code);
    }

    [Fact]
    public async Task A_host_scope_write_is_refused_with_a_Result_failure()
    {
        // No tenant claim means host scope, where the query filter is off and a new row would have no
        // owner. The catalog is tenant-only, so the write is refused rather than stamped with Guid.Empty.
        var handler = new CreateCategoryCommandHandler(
            new FakeCategoryRepository(),
            new FakeCurrentTenant(Guid.Empty));

        var result = await handler.HandleAsync(new CreateCategoryCommand("Tools"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Category.TenantRequired", result.Error.Code);
    }

    [Fact]
    public async Task A_host_scope_read_is_refused_with_a_Result_failure()
    {
        // A read at host scope would return every tenant's categories, so it is refused too — the AC
        // only names writes, but answering this one would be a cross-tenant read.
        var writeHandler = new CreateCategoryCommandHandler(new FakeCategoryRepository(), new FakeCurrentTenant(TenantId));
        await writeHandler.HandleAsync(new CreateCategoryCommand("Tools"), CancellationToken.None);

        var handler = new GetCategoriesQueryHandler(new FakeCategoryRepository(), new FakeCurrentTenant(Guid.Empty));

        var result = await handler.HandleAsync(
            new GetCategoriesQuery(1, 20, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task Reading_an_unknown_id_is_NotFound()
    {
        var handler = new GetCategoryQueryHandler(new FakeCategoryRepository(), new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(new GetCategoryQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NotFound", result.Error.Code);
    }

    [Theory]
    [InlineData(0, 20, "Category.InvalidPageNumber")]
    [InlineData(1, 0, "Category.InvalidPageSize")]
    [InlineData(1, 101, "Category.InvalidPageSize")]
    public async Task Paging_outside_1_to_100_is_a_Validation_failure(int pageNumber, int pageSize, string code)
    {
        var handler = new GetCategoriesQueryHandler(new FakeCategoryRepository(), new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(
            new GetCategoriesQuery(pageNumber, pageSize, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(code, result.Error.Code);
    }

    [Fact]
    public async Task A_valid_page_returns_the_items_and_the_counts()
    {
        var handler = new GetCategoriesQueryHandler(
            new FakeCategoryRepository(
                Category.Create(Guid.NewGuid(), TenantId, "Tools"),
                Category.Create(Guid.NewGuid(), TenantId, "Hardware")),
            new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(new GetCategoriesQuery(1, 20, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task Renaming_a_category_to_a_name_another_category_uses_is_a_Conflict_failure()
    {
        var target = Category.Create(Guid.NewGuid(), TenantId, "Tools");
        var other = Category.Create(Guid.NewGuid(), TenantId, "Hardware");
        var handler = new RenameCategoryCommandHandler(
            new FakeCategoryRepository(target, other),
            new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(
            new RenameCategoryCommand(target.Id, "hardware"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Renaming_a_missing_category_is_NotFound()
    {
        var handler = new RenameCategoryCommandHandler(new FakeCategoryRepository(), new FakeCurrentTenant(TenantId));

        var result = await handler.HandleAsync(
            new RenameCategoryCommand(Guid.NewGuid(), "Tools"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public void Renaming_keeps_the_creation_stamp_and_raises_the_rename_event()
    {
        var category = Category.Create(Guid.NewGuid(), TenantId, "Tools");
        var createdOn = category.CreatedOn;

        category.Rename("Hardware");

        Assert.Equal("Hardware", category.Name);
        Assert.Equal("HARDWARE", category.NormalizedName);
        Assert.Equal(createdOn, category.CreatedOn);
        Assert.Contains(category.DomainEvents, domainEvent => domainEvent is CategoryRenamedDomainEvent);
    }

    [Fact]
    public void Creating_a_category_raises_the_created_event()
    {
        var category = Category.Create(Guid.NewGuid(), TenantId, "Tools");

        Assert.Contains(category.DomainEvents, domainEvent => domainEvent is CategoryCreatedDomainEvent);
    }

    [Fact]
    public void A_category_cannot_be_created_without_a_tenant()
    {
        // The handler refuses this with a Result; the aggregate throws, because reaching it means a bug.
        Assert.Throws<ArgumentException>(() => Category.Create(Guid.NewGuid(), Guid.Empty, "Tools"));
    }

    private static Error ErrorNamed(string factory)
    {
        var id = Guid.NewGuid();

        return factory switch
        {
            nameof(CategoryErrors.NotFound) => CategoryErrors.NotFound(id),
            nameof(CategoryErrors.NameAlreadyExists) => CategoryErrors.NameAlreadyExists("Tools"),
            nameof(CategoryErrors.NameRequired) => CategoryErrors.NameRequired(),
            nameof(CategoryErrors.NameTooLong) => CategoryErrors.NameTooLong(Category.MaxNameLength),
            nameof(CategoryErrors.InvalidPageNumber) => CategoryErrors.InvalidPageNumber(),
            nameof(CategoryErrors.InvalidPageSize) => CategoryErrors.InvalidPageSize(GetCategoriesQuery.MaxPageSize),
            nameof(CategoryErrors.TenantRequired) => CategoryErrors.TenantRequired(),
            nameof(CategoryErrors.CategoryHasProducts) => CategoryErrors.CategoryHasProducts(id),
            _ => throw new ArgumentOutOfRangeException(nameof(factory), factory, "Unknown category error."),
        };
    }

    /// <summary>An in-memory stand-in: enough to drive the handlers, and deliberately tenant-blind.</summary>
    private sealed class FakeCategoryRepository(params Category[] categories) : ICategoryRepository
    {
        private readonly List<Category> _categories = [.. categories];

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
