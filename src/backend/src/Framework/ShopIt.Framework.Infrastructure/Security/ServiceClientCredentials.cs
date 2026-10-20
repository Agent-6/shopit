using Microsoft.Extensions.Configuration;

namespace ShopIt.Framework.Infrastructure.Security;

/// <summary>
/// Credentials for one OAuth2 client that services use to talk to each other: the shared secret the
/// authentication server registers the client with, and that every caller presents.
/// </summary>
/// <remarks>
/// <para>
/// These used to be string literals in five places — the auth server's client seeding, two token
/// handlers, two token-validation registrations and an internal authorization policy — so rotating the
/// secret meant editing five files and every value was committed to a public repository.
/// </para>
/// <para>
/// The <strong>client id</strong> stays in each service's <c>appsettings.json</c>: it identifies a
/// client, it is not a credential, and it is useful to read. The <strong>secret</strong> never appears
/// in configuration files. <c>ShopIt.AppHost</c> generates one per run and injects it into every
/// service as environment configuration, so the local stack still starts with no manual setup.
/// </para>
/// <para>
/// A missing secret fails at startup with a message that names the section. That is deliberate: a
/// service that silently cannot authenticate is harder to diagnose than one that refuses to start.
/// </para>
/// </remarks>
/// <param name="ClientId">The OAuth2 client id.</param>
/// <param name="ClientSecret">The client's secret.</param>
public sealed record ServiceClientCredentials(string ClientId, string ClientSecret)
{
    /// <summary>
    /// Reads credentials from <c>{section}:ClientId</c> and <c>{section}:ClientSecret</c>.
    /// </summary>
    /// <param name="configuration">The configuration to read.</param>
    /// <param name="section">Configuration section holding the pair, e.g. <c>InternalClients:Backend</c>.</param>
    /// <param name="purpose">What the client is for, used in the failure message.</param>
    /// <returns>The client's credentials.</returns>
    /// <exception cref="InvalidOperationException">The client id or secret is not configured.</exception>
    public static ServiceClientCredentials Read(IConfiguration configuration, string section, string purpose)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var clientId = configuration[$"{section}:ClientId"];
        var clientSecret = configuration[$"{section}:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                $"Missing configuration '{section}:ClientId' (the {purpose} client). " +
                "Add it to the service's appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                $"Missing configuration '{section}:ClientSecret' (the {purpose} client). " +
                "ShopIt.AppHost generates this for local development; run the service through " +
                "`dotnet run --project ShopIt.AppHost`, or supply the value yourself when running " +
                "the service on its own.");
        }

        return new ServiceClientCredentials(clientId, clientSecret);
    }
}
