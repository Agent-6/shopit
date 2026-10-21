using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ShopIt.Framework.Presentation.Authorization;

/// <summary>
/// The authorization policy that admits <em>service callers</em> — and only service callers — to a
/// <c>/api/internal</c> endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The distinction the policy makes is the shape of the token's subject. A client-credentials token
/// carries the OAuth2 client id as <c>sub</c> (a string such as <c>shopit-backend</c>); an interactive
/// user's token carries that user's id, which is a GUID. So "subject is not a GUID <em>and</em> is the
/// backend client" admits the backend and refuses every user token, including an administrator's.
/// </para>
/// <para>
/// It lives in the framework rather than in each service because it is one rule shared by every service
/// that exposes an internal surface (Identity, then Tenancy): two copies of an authorization rule is how
/// one of them ends up weaker than the other without anybody noticing. The service still supplies its own
/// client id from its own configuration — the framework does not read configuration on a service's behalf.
/// </para>
/// </remarks>
public static class InternalServicePolicy
{
    /// <summary>The policy's registered name.</summary>
    public const string Name = "InternalOnly";

    /// <summary>
    /// Registers <see cref="Name"/> so that only a client-credentials token issued to
    /// <paramref name="serviceClientId"/> satisfies it.
    /// </summary>
    /// <param name="options">The authorization options being configured.</param>
    /// <param name="serviceClientId">
    /// The backend client's id (for example <c>shopit-backend</c>), read from the service's own
    /// <c>InternalClients:Backend</c> configuration.
    /// </param>
    /// <returns>The same <paramref name="options"/>, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="serviceClientId"/> is empty.</exception>
    public static AuthorizationOptions AddInternalServicePolicy(
        this AuthorizationOptions options,
        string serviceClientId)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceClientId);

        options.AddPolicy(Name, policy =>
        {
            policy.RequireAuthenticatedUser();

            // Not a GUID means "this is not a person": a user token's subject is always the user's GUID.
            // Comparing the id as well means a *user* named like the client id still cannot pass.
            policy.RequireAssertion(context =>
                context.User.FindFirstValue("sub") is { } subject
                && !Guid.TryParse(subject, out _)
                && string.Equals(subject, serviceClientId, StringComparison.Ordinal));
        });

        return options;
    }
}
