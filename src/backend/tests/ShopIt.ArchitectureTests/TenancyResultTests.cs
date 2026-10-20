using ShopIt.Framework.Domain.Errors;
using ShopIt.Tenancy.Application.Tenants.Commands.UpdateTenant;
using ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;
using ShopIt.Tenancy.Domain.Entities;
using ShopIt.Tenancy.Domain.Repositories;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards the Tenancy pilot of #32: expected outcomes come back as <c>Result</c> failures, not
/// exceptions, and each failure carries the category the presentation layer maps to a status code.
/// </summary>
public class TenancyResultTests
{
    [Fact]
    public async Task A_missing_tenant_is_a_NotFound_failure_not_an_exception()
    {
        var handler = new GetTenantQueryHandler(new FakeTenantRepository());

        var result = await handler.HandleAsync(new GetTenantQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Tenant.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task A_found_tenant_returns_the_value()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Acme");
        var handler = new GetTenantQueryHandler(new FakeTenantRepository(tenant));

        var result = await handler.HandleAsync(new GetTenantQuery(tenant.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme", result.Value.Name);
    }

    [Fact]
    public async Task Renaming_to_a_taken_name_is_a_Conflict_failure()
    {
        var target = Tenant.Create(Guid.NewGuid(), "Acme");
        var other = Tenant.Create(Guid.NewGuid(), "Globex");
        var handler = new UpdateTenantCommandHandler(new FakeTenantRepository(target, other));

        var result = await handler.HandleAsync(new UpdateTenantCommand(target.Id, "Globex"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Updating_a_missing_tenant_is_a_NotFound_failure()
    {
        var handler = new UpdateTenantCommandHandler(new FakeTenantRepository());

        var result = await handler.HandleAsync(new UpdateTenantCommand(Guid.NewGuid(), "Acme"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    private sealed class FakeTenantRepository(params Tenant[] tenants) : ITenantRepository
    {
        private readonly List<Tenant> _tenants = [.. tenants];

        public Task<Tenant?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_tenants.FirstOrDefault(t => t.Id == id));

        public Task<Tenant> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The migrated handlers must use FindByIdAsync.");

        public Task AddAsync(Tenant entity, CancellationToken cancellationToken = default)
        {
            _tenants.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Tenant entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveAsync(Tenant entity, CancellationToken cancellationToken = default)
        {
            _tenants.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(_tenants.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<(IEnumerable<Tenant> Items, long TotalCount, long TotalPages)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IEnumerable<Tenant>, long, long)>((_tenants, _tenants.Count, 1));
    }
}
