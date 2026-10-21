using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ShopIt.Framework.Core.CQRS;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Framework.Presentation.Results;
using ShopIt.Tenancy.Application.Tenants.Queries.GetTenant;
using ShopIt.Tenancy.Client.Models;

namespace ShopIt.Tenancy.Presentation.Internal;

/// <summary>
/// The tenant registry's internal surface: what other services ask it synchronously.
/// </summary>
/// <remarks>
/// <para>
/// The currency is here rather than in the access token because a token is a statement about the <em>caller</em>,
/// and every tenant's currency in it would put shop data in every user's token and make a currency change wait for
/// the token to expire (ADR-0006 records the alternatives and why they were rejected).
/// </para>
/// <para>
/// The group carries <see cref="InternalServicePolicy.Name"/>, the same policy Identity's internal API uses, so only
/// a client-credentials token issued to the backend client can reach it — an administrator's user token cannot.
/// </para>
/// </remarks>
public static class InternalEndpoints
{
    /// <summary>Maps the internal API under <c>/api/internal</c>.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal")
            .RequireAuthorization(InternalServicePolicy.Name)
            .WithTags("Internal");

        group.MapGet("/tenants/{tenantId:guid}/currency", GetTenantCurrency);

        return app;
    }

    /// <summary>
    /// Serves one tenant's currency to a service that must denominate a price in it.
    /// </summary>
    /// <remarks>
    /// It goes through <see cref="GetTenantQuery"/> rather than reading the repository directly, so "unknown tenant"
    /// has exactly one definition in this service and the failure reaches the caller as the same 404 the public API
    /// returns. The catalog turns that into a refused write, never into a guessed currency.
    /// </remarks>
    private static async Task<IResult> GetTenantCurrency(
        Guid tenantId,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.QueryAsync(new GetTenantQuery(tenantId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return Results.Ok(new TenantCurrencyResponse(result.Value.Id, result.Value.Currency));
    }
}
