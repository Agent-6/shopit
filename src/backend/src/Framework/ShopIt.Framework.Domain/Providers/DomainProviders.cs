namespace ShopIt.Framework.Domain.Providers;

/// <summary>
/// Static gateway for accessing Guid and Date providers in domain models and tests.
/// </summary>
public static class DomainProviders
{
    private static IGuidProvider _guid = new GuidProvider();
    private static IDateProvider _date = new DateProvider();

    /// <summary>
    /// Gets or sets the global GUID provider.
    /// </summary>
    public static IGuidProvider Guid
    {
        get => _guid;
        set => _guid = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the global date and time provider.
    /// </summary>
    public static IDateProvider Date
    {
        get => _date;
        set => _date = value ?? throw new ArgumentNullException(nameof(value));
    }
}
