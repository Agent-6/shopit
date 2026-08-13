namespace ShopIt.Identity.Application.DataSeeding;

/// <summary>
/// Options controlling the automatic data seeding performed by the Identity service.
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Password assigned to automatically provisioned admin accounts (e.g. the tenant
    /// admin created when a new tenant is provisioned).
    /// </summary>
    public string AdminPassword { get; set; } = "P@SSw0rd";
}
