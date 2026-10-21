using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Persistence.Data;
using ShopIt.Identity.Persistence.Repositories;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Covers the one account lookup that must cross tenants: signing in (#92).
/// </summary>
/// <remarks>
/// <para>
/// Every other read in Identity is tenant-filtered, and that is correct. Authentication is the exception
/// because it runs <em>before</em> a tenant is known: the account's tenant is the result of the lookup, not
/// an input to it. So what is proven here is the property that makes it correct — the answer does not
/// depend on the ambient scope at all — and the one case where it must refuse rather than answer.
/// </para>
/// <para>
/// <strong>InMemory, deliberately.</strong> No database and no stack is needed to exercise a query filter;
/// the filter is a model artefact evaluated per query by the query pipeline, exactly as on PostgreSQL (see
/// <see cref="CatalogTestDatabase"/>, which argues the same point for isolation). What is <em>not</em>
/// covered here is <c>UserManager</c> itself — password hashing and the Identity stores — which needs a real
/// user store and is verified live.
/// </para>
/// </remarks>
public class TenantLoginLookupTests
{
    private static readonly TenantInfo HostScope = new(Guid.Empty, "Host");

    /// <summary>The demo tenant the manager and administrator accounts are seeded into.</summary>
    private static readonly Guid DemoTenantId = Guid.Parse("b5d0c0e4-3a5b-4cdc-8d2a-7f1f6c9f5b4e");

    /// <summary>A second, unrelated tenant — the one a wrong answer would leak from.</summary>
    private static readonly Guid OtherTenantId = Guid.Parse("0f3e6a1c-9d24-4f8b-8a51-2c7b6e0d4a19");

    [Fact]
    public async Task A_client_credentials_caller_finds_a_tenant_account()
    {
        // The ambient scope of a service-to-service call: authenticated, but with no tenant claim.
        await using var context = CreateContext(new FakeCurrentTenant(HostScope));
        await AddUserAsync(context, "tenant@user.com", DemoTenantId);

        var user = await CreateLookup(context).FindForLoginAsync("tenant@user.com");

        Assert.NotNull(user);
        Assert.Equal(DemoTenantId, user.TenantId);
    }

    [Fact]
    public async Task The_login_identifier_is_case_insensitive()
    {
        await using var context = CreateContext(new FakeCurrentTenant(HostScope));
        await AddUserAsync(context, "tenant@user.com", DemoTenantId);

        var user = await CreateLookup(context).FindForLoginAsync("TENANT@User.com");

        Assert.NotNull(user);
        Assert.Equal(DemoTenantId, user.TenantId);
    }

    [Fact]
    public async Task The_answer_does_not_depend_on_the_ambient_scope()
    {
        // This is the defect #92 describes: a tenant-filtered lookup answers a different question depending
        // on who is asking. Host scope happens to see every tenant's rows — the filter is disabled there —
        // while a tenant scope sees that tenant's accounts only. Resolving an account must give the same
        // answer in all three scopes, because sign-in happens before any of them is known.
        var currentTenant = new FakeCurrentTenant(HostScope);

        await using var context = CreateContext(currentTenant);
        await AddUserAsync(context, "tenant@user.com", DemoTenantId);

        var scopes = new[]
        {
            HostScope,
            new TenantInfo(DemoTenantId, "Demo"),
            new TenantInfo(OtherTenantId, "Other"),
        };

        foreach (var scope in scopes)
        {
            using (currentTenant.Change(scope))
            {
                var user = await CreateLookup(context).FindForLoginAsync("tenant@user.com");

                Assert.True(
                    user is not null,
                    $"The login lookup must resolve the account in {scope.Name} scope; it did not.");
                Assert.Equal(DemoTenantId, user.TenantId);
            }
        }
    }

    [Fact]
    public async Task An_unknown_identifier_resolves_to_nothing()
    {
        await using var context = CreateContext(new FakeCurrentTenant(HostScope));
        await AddUserAsync(context, "tenant@user.com", DemoTenantId);

        var user = await CreateLookup(context).FindForLoginAsync("nobody@user.com");

        Assert.Null(user);
    }

    [Fact]
    public async Task An_identifier_shared_by_two_tenants_is_refused_rather_than_guessed()
    {
        // Identity's unique-email rule is enforced per tenant (its validator queries the filtered set), so
        // the same address in two tenants is possible. Signing in cannot then be unambiguous, and picking
        // one would put the caller in a tenant they may not own — so the lookup refuses, and the endpoint
        // answers with the same generic failure it gives a wrong password.
        await using var context = CreateContext(new FakeCurrentTenant(HostScope));
        await AddUserAsync(context, "shared@user.com", DemoTenantId);
        await AddUserAsync(context, "shared@user.com", OtherTenantId);

        var user = await CreateLookup(context).FindForLoginAsync("shared@user.com");

        Assert.Null(user);
    }

    private static ApplicationDbContext CreateContext(FakeCurrentTenant currentTenant) =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"identity-login-lookup-{Guid.NewGuid()}")
                .Options,
            currentTenant);

    private static async Task AddUserAsync(ApplicationDbContext context, string email, Guid tenantId)
    {
        // Emails are used as usernames throughout the seed, which is what the login form submits.
        context.Users.Add(User.Create(Guid.NewGuid(), email, email, tenantId));
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// The repository under test. <c>UpperInvariantLookupNormalizer</c> is what <c>AddIdentityCore</c>
    /// registers, so this is the normalisation production runs with — the values in
    /// <c>NormalizedUserName</c>/<c>NormalizedEmail</c> depend on it.
    /// </summary>
    private static UserRepository CreateLookup(ApplicationDbContext context) =>
        new(context, new UpperInvariantLookupNormalizer(), NullLogger<UserRepository>.Instance);
}
