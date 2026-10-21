using ShopIt.Framework.Domain.Tenancy;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// A controllable <see cref="ICurrentTenant"/> for tests: the scope is whatever the last
/// <see cref="Change"/> set, and a test can move it as often as it likes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>It throws when no scope has been set.</strong> That is deliberate, and it mirrors the
/// production <c>CurrentTenant</c>: silently answering <see cref="Guid.Empty"/> would turn every
/// unscoped test into a host-scoped one, and host scope <em>disables</em> the tenant filter (ADR-0001) —
/// so a forgetful test would pass by seeing everything. Failing here makes the scope explicit in every
/// test that queries.
/// </para>
/// <para>
/// Nesting unwinds in reverse order, like the production implementation and like
/// <see cref="Change"/>'s contract.
/// </para>
/// </remarks>
internal sealed class FakeCurrentTenant : ICurrentTenant
{
    private readonly Stack<TenantInfo> _scopes = new();

    /// <summary>Creates a tenant with no scope; a query before <see cref="Change"/> throws.</summary>
    public FakeCurrentTenant()
    {
    }

    /// <summary>
    /// Creates a tenant that already acts as <paramref name="scope"/> for its whole lifetime.
    /// </summary>
    /// <remarks>
    /// For the tests that give each <c>DbContext</c> instance its own context object: the scope must not
    /// be movable there, because what is being proven is that two <em>instances</em> — not one ambient
    /// switch — each filter by their own tenant.
    /// </remarks>
    public FakeCurrentTenant(TenantInfo scope) => _scopes.Push(scope);

    public Guid Id => Current.Id;

    public string? Name => Current.Name;

    private TenantInfo Current =>
        _scopes.Count > 0
            ? _scopes.Peek()
            : throw new InvalidOperationException(
                "Tenant context missing. Call Change() for background tasks.");

    public IDisposable Change(TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        _scopes.Push(tenant);

        return new Scope(_scopes);
    }

    private sealed class Scope(Stack<TenantInfo> scopes) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            scopes.Pop();
        }
    }
}
