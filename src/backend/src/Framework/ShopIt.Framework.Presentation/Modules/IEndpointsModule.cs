using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace ShopIt.Framework.Presentation.Modules;

public abstract class EndpointsModule
{
    public abstract string GroupDisplayName { get; }
    public abstract RoutePattern GroupPrefix { get; }

    public abstract void RegisterEndpoints(IEndpointRouteBuilder app);

    internal void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(GroupPrefix);
        RegisterEndpoints(group);
    }
}
