using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Infrastructure.Providers;

/// <summary>
/// Default implementation of <see cref="IGuidProvider"/> using standard System.Guid.
/// </summary>
internal class GuidProvider : IGuidProvider
{
    /// <inheritdoc />
    public Guid NewGuid() => Guid.NewGuid();
}
