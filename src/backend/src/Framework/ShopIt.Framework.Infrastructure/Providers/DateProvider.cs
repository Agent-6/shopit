using ShopIt.Framework.Domain.Providers;

namespace ShopIt.Framework.Infrastructure.Providers;

/// <summary>
/// Default implementation of <see cref="IDateProvider"/> using standard System.DateTime.
/// </summary>
internal class DateProvider : IDateProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime Today => DateTime.Today;
}
