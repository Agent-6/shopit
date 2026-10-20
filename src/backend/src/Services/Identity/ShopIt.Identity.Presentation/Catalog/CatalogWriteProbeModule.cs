using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Framework.Presentation.Modules;

namespace ShopIt.Identity.Presentation.Catalog;

/// <summary>
/// **PLACEHOLDER (#24).** One endpoint whose only job is to be gated on <c>catalog.write</c>, a
/// permission Identity does not declare: it arrives from the placeholder catalog provider in the
/// Notifications service. That is the property the epic needs — declaring a permission and enforcing
/// it are different services' jobs, and enforcement must work for a name the enforcing service has
/// never heard of.
/// </summary>
/// <remarks>
/// It lives in Identity rather than in the service that will own the catalog because Tenancy's
/// permission handler resolves over HTTP through a Refit client that is currently broken at runtime
/// (every authorised Tenancy endpoint returns 500 — see the follow-up issue). Identity resolves
/// in-database, so it can actually demonstrate 200/403 today. Move the proof endpoint to the catalog
/// service once it exists.
/// </remarks>
public class CatalogWriteProbeModule : EndpointsModule
{
    /// <summary>The permission under test, as a literal — the point is that Identity does not declare it.</summary>
    public const string WritePermission = "catalog.write";

    public override string GroupDisplayName => "Catalog (placeholder)";

    public override RoutePattern GroupPrefix => RoutePatternFactory.Parse("/catalog-probe");

    public override void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        // 200 for a principal holding catalog.write, 403 for one that does not. The body carries
        // nothing on purpose: this endpoint proves authorization, not catalog behaviour.
        app.MapGet("/write", GetWriteProbe).RequirePermission(WritePermission);
    }

    private static IResult GetWriteProbe() =>
        Results.Ok(new
        {
            granted = WritePermission,
            note = "Placeholder endpoint: reachable only with catalog.write (#24).",
        });
}
