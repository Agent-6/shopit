namespace ShopIt.Framework.Domain.Providers;

/// <summary>
/// Default implementation of <see cref="IGuidProvider"/> using standard System.Guid.
/// </summary>
public class GuidProvider : IGuidProvider
{
    /// <inheritdoc />
    public Guid NewGuid() => Guid.NewGuid();
}
