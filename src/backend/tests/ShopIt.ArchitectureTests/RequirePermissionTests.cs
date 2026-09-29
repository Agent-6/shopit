using Microsoft.AspNetCore.Authorization;
using ShopIt.Framework.Presentation.Authorization;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers <see cref="RequirePermissionExtensions"/> after it moved into the framework from the two
/// service copies that used to exist.
/// </summary>
/// <remarks>
/// <para>
/// Scope, stated honestly: this verifies that the shared extension builds the correct requirement.
/// It does <strong>not</strong> verify the 403/200 outcome of an authorised request, because that is
/// decided by each service's <c>IAuthorizationHandler</c> — which this change does not touch — and
/// exercising it would need the Aspire stack running with an authenticated principal.
/// </para>
/// <para>
/// The substantive guarantee here is that the moved code still behaves as it did in both copies.
/// </para>
/// </remarks>
public class RequirePermissionTests
{
    [Fact]
    public void RequirePermission_adds_a_requirement_carrying_the_permission_name()
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequirePermission("catalog.write")
            .Build();

        var requirement = Assert.Single(policy.Requirements.OfType<PermissionRequirement>());

        Assert.Equal("catalog.write", requirement.PermissionName);
    }

    [Fact]
    public void RequirePermission_does_not_imply_authentication_by_itself()
    {
        // Only the permission requirement is added. Requiring an authenticated user is the endpoint
        // overload's job; adding it here would silently change the behaviour of every guarded endpoint.
        var policy = new AuthorizationPolicyBuilder()
            .RequirePermission("catalog.write")
            .Build();

        var requirement = Assert.Single(policy.Requirements);
        Assert.IsType<PermissionRequirement>(requirement);
    }

    [Fact]
    public void Multiple_permissions_accumulate_rather_than_replace()
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequirePermission("catalog.view")
            .RequirePermission("catalog.write")
            .Build();

        var names = policy.Requirements
            .OfType<PermissionRequirement>()
            .Select(r => r.PermissionName)
            .ToList();

        Assert.Equal(["catalog.view", "catalog.write"], names);
    }
}
