namespace ShopIt.Framework.Infrastructure;

/// <summary>
/// Runs <paramref name="action"/> when disposed, exactly once.
/// </summary>
/// <remarks>
/// Used to unwind a scope change — see <see cref="Tenancy.CurrentTenant.Change"/>.
/// </remarks>
public sealed class DisposeAction(Action action) : IDisposable
{
    private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));
    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            _action();
            _disposed = true;
        }
    }
}
