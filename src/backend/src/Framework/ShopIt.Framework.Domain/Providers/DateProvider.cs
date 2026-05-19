namespace ShopIt.Framework.Domain.Providers;

/// <summary>
/// Default implementation of <see cref="IDateProvider"/> using standard System.DateTime.
/// </summary>
public class DateProvider : IDateProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime Today => DateTime.Today;
}
