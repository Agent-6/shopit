namespace ShopIt.Framework.Domain.Providers;

/// <summary>
/// Static gateway for accessing Guid and Date providers in domain models.
/// </summary>
internal static class DomainProviders
{
    /// <summary>
    /// The global GUID provider.
    /// </summary>
    internal static IGuidProvider Guid { get; private set; } = default!;

    /// <summary>
    /// The global date and time provider.
    /// </summary>
    internal static IDateProvider Date { get; private set; } = default!;

    /// <summary>
    /// Internal method to set the global providers (used by the DI extension).
    /// </summary>
    /// <param name="guidProvider">The GUID provider to set.</param>
    /// <param name="dateProvider">The date provider to set.</param>
    internal static void SetProviders(IGuidProvider guidProvider, IDateProvider dateProvider)
    {
        ArgumentNullException.ThrowIfNull(guidProvider, nameof(guidProvider));
        ArgumentNullException.ThrowIfNull(dateProvider, nameof(dateProvider));

        Guid = guidProvider;
        Date = dateProvider;
    }
}
