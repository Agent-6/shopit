namespace ShopIt.Framework.Domain.Providers;

/// <summary>
/// Defines a provider for obtaining date and time values.
/// This interface abstracts time to allow for reliable unit testing by freezing or shifting time.
/// </summary>
public interface IDateProvider
{
    /// <summary>
    /// Gets the current date and time in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current date.
    /// </summary>
    DateTime Today { get; }
}
