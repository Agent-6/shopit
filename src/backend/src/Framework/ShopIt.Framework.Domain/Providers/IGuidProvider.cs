namespace ShopIt.Framework.Domain.Providers;

/// <summary>
/// Defines a provider for generating Guid values.
/// This interface abstracts Guid generation to allow for reliable unit testing.
/// </summary>
public interface IGuidProvider
{
    /// <summary>
    /// Generates a new Guid.
    /// </summary>
    /// <returns>A new Guid instance.</returns>
    Guid NewGuid();
}
