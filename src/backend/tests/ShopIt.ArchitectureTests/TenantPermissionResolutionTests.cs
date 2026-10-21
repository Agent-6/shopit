using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ShopIt.Catalog.Presentation.Authorization;
using ShopIt.Framework.Application.Caching;
using ShopIt.Framework.Infrastructure.Tenancy;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Identity.Client.Services;
using ShopIt.Tenancy.Presentation.Authorization;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers the tenant travelling from the caller's token to Identity's permission resolution (#86).
/// </summary>
/// <remarks>
/// <para>
/// The defect was that a service resolved a tenant user's permissions in <em>host</em> scope: the call to
/// Identity carries client credentials, so it has no tenant claim of its own, and the user's role-derived
/// grants then came from the host copies of their roles — or from none at all. Admin-shaped users were
/// unaffected, which is why it went unnoticed.
/// </para>
/// <para>
/// What is proven here is the wiring of that fix: the request's own tenant (from its <c>tenant_id</c> claim,
/// through the same <see cref="CurrentTenant"/> the query filters use) is what the client is asked about, and
/// the cache key that answer is stored under includes that tenant. It does not exercise Identity's side of the
/// hop — that needs the stack, and is verified live.
/// </para>
/// </remarks>
public class TenantPermissionResolutionTests
{
    private static readonly Guid DemoTenantId = Guid.Parse("b5d0c0e4-3a5b-4cdc-8d2a-7f1f6c9f5b4e");
    private static readonly Guid UserId = Guid.Parse("3f8c1b62-5a4d-4a3e-9b7d-1e2f3a4b5c6d");

    [Fact]
    public async Task The_catalog_handler_resolves_permissions_in_the_callers_tenant()
    {
        var client = new RecordingPermissionClient("catalog.view");

        var context = await HandleAsync(
            tenant => new CatalogPermissionAuthorizationHandler(client, tenant),
            DemoTenantId,
            "catalog.view");

        Assert.True(context.HasSucceeded);
        Assert.Equal(DemoTenantId, client.RequestedTenantId);
        Assert.Equal(UserId, client.RequestedUserId);
    }

    [Fact]
    public async Task The_tenancy_handler_resolves_permissions_in_the_callers_tenant()
    {
        var client = new RecordingPermissionClient("tenant.view");

        var context = await HandleAsync(
            tenant => new TenantPermissionAuthorizationHandler(client, tenant),
            DemoTenantId,
            "tenant.view");

        Assert.True(context.HasSucceeded);
        Assert.Equal(DemoTenantId, client.RequestedTenantId);
    }

    [Fact]
    public async Task A_caller_without_a_tenant_claim_is_still_resolved_as_host()
    {
        // A host user's behaviour is unchanged: no tenant claim means host scope, which is what it was
        // before the tenant was sent, and what every other Identity endpoint means by a missing claim.
        var client = new RecordingPermissionClient("catalog.view");

        var context = await HandleAsync(
            tenant => new CatalogPermissionAuthorizationHandler(client, tenant),
            tenantId: null,
            "catalog.view");

        Assert.True(context.HasSucceeded);
        Assert.Equal(Guid.Empty, client.RequestedTenantId);
    }

    [Fact]
    public async Task A_permission_the_tenant_does_not_grant_is_not_honoured()
    {
        // The negative half: the tenant is not a cache-busting decoration, the resolved set still decides.
        var client = new RecordingPermissionClient("catalog.view");

        var context = await HandleAsync(
            tenant => new CatalogPermissionAuthorizationHandler(client, tenant),
            DemoTenantId,
            "catalog.write");

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public void The_permission_cache_key_is_per_tenant()
    {
        // Rule derived from #86: once resolution is tenant-aware, a key without the tenant is a cache
        // collision that answers one tenant with another tenant's grants. This is the assertion that fails
        // if the key ever loses it again.
        var inDemoTenant = CacheKeys.UserPermissions(UserId, DemoTenantId);
        var inOtherTenant = CacheKeys.UserPermissions(UserId, Guid.NewGuid());
        var inHostScope = CacheKeys.UserPermissions(UserId, Guid.Empty);

        Assert.NotEqual(inDemoTenant, inOtherTenant);
        Assert.NotEqual(inDemoTenant, inHostScope);
        Assert.NotEqual(inHostScope, inOtherTenant);

        // Same inputs, same key — otherwise the writer and the reader disagree and the cache is two caches.
        Assert.Equal(inDemoTenant, CacheKeys.UserPermissions(UserId, DemoTenantId));
    }

    private static async Task<AuthorizationHandlerContext> HandleAsync(
        Func<CurrentTenant, IAuthorizationHandler> createHandler,
        Guid? tenantId,
        string permissionName)
    {
        var principal = PrincipalFor(tenantId);

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(permissionName)],
            principal,
            resource: null);

        // The production CurrentTenant, driven by the same principal the handler will see — so the path from
        // the caller's tenant_id claim to the tenant sent to Identity is exercised, not stubbed.
        var currentTenant = new CurrentTenant(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });

        await createHandler(currentTenant).HandleAsync(context);

        return context;
    }

    private static ClaimsPrincipal PrincipalFor(Guid? tenantId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, UserId.ToString()) };

        if (tenantId is { } id)
        {
            claims.Add(new Claim("tenant_id", id.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    /// <summary>Records what the handler asked for, so the tenant can be asserted rather than inferred.</summary>
    private sealed class RecordingPermissionClient(params string[] granted) : IIdentityPermissionClient
    {
        public Guid? RequestedUserId { get; private set; }
        public Guid? RequestedTenantId { get; private set; }

        public Task<UserPermissionsSnapshot> GetPermissionsAsync(
            Guid userId,
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            RequestedTenantId = tenantId;

            return Task.FromResult(new UserPermissionsSnapshot(false, granted));
        }
    }
}
