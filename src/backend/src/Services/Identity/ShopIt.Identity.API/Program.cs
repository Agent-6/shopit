using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using ShopIt.Authentication.Events;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Framework.Presentation;
using ShopIt.Framework.Presentation.Authorization;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Permissions;
using ShopIt.Framework.Infrastructure;
using ShopIt.Framework.Infrastructure.Security;
using ShopIt.Identity.Application;
using ShopIt.Identity.Events;
using ShopIt.Identity.Application.DataSeeding;
using ShopIt.Identity.Application.Notifications;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Permissions;
using ShopIt.Identity.Domain.Roles;
using ShopIt.Framework.Domain.Tenancy;
using ShopIt.Identity.Persistence;
using ShopIt.Identity.Persistence.Data;
using ShopIt.Identity.Persistence.Permissions;
using ShopIt.Identity.Persistence.Stores;
using ShopIt.Identity.Presentation;
using ShopIt.Identity.Presentation.Authorization;
using ShopIt.Identity.Presentation.Internal;
using ShopIt.Tenancy.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// request handlers
builder.Services.AddDomainServices();
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddApplication();

// Where the Notification email links (activation / password reset) point. The pages
// live in the Authentication service, so its public base URL is configured here.
builder.Services.AddOptions<EmailNotificationOptions>()
    .Bind(builder.Configuration.GetSection(EmailNotificationOptions.SectionName));

builder.Services.AddOptions<SeedOptions>()
    .Bind(builder.Configuration.GetSection(SeedOptions.SectionName));

builder.Services.AddPersistence(
    "identity-db",
    builder.Configuration,
    InboxSubscription.Consuming(
        typeof(ForgotPasswordRequestedIntegrationEvent),
        typeof(PasswordResetRequestedIntegrationEvent),
        typeof(EmailConfirmationOtpRequestedIntegrationEvent),
        typeof(EmailConfirmationSubmittedIntegrationEvent),
        typeof(ResendInvitationRequestedIntegrationEvent),
        typeof(TenantCreatedIntegrationEvent),
        typeof(PermissionCatalogPublishedIntegrationEvent)),
    handlerAssemblies: typeof(ShopIt.Identity.Application.DependencyInjection).Assembly);

// Identity owns the permission registry, so its own catalog is applied in place at startup
// instead of being published over Kafka. Other services publish theirs; both go through the
// same hook, so no service carries bespoke catalog-publishing code.
builder.Services.AddPermissionCatalogPublishing<
    SynchronizingPermissionCatalogPublicationSink,
    ShopItIdentityPermissionDefinitionProvider>(
    ShopItIdentityPermissionDefinitionProvider.SourceService);
// TODO: move this to the persistence extension method,and make it an extension method on WebApplicationBuilder
builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<User>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings (for password validation, not cookies)
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<Role>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Replace the default Identity stores with tenant-aware implementations (see
// Persistence/Stores): role-name lookups must resolve within a tenant (the default
// lookups are ambiguous at host scope where same-named host/tenant roles coexist),
// and rows created by the stores (role joins, claims) are stamped with the tenant id.
builder.Services.AddScoped<IRoleStore<Role>, TenantRoleStore>();
builder.Services.AddScoped<IUserStore<User>, TenantUserStore>();

// Client credentials for service-to-service calls. Secrets are injected by the AppHost; ids come from
// appsettings. A missing secret fails here, at startup, instead of as an unexplained 401 later.
var introspectionClient = ServiceClientCredentials.Read(
    builder.Configuration, "InternalClients:Introspection", "token introspection");
var backendClient = ServiceClientCredentials.Read(
    builder.Configuration, "InternalClients:Backend", "backend service");

// Add OpenIddict validation
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Point to your Auth Server
        options.SetIssuer("https://localhost:7234/");  // Your Auth Server URL

        // Add the audience that matches the token's audience
        //options.AddAudiences("angular-spa");  // Same as your Angular client ID

        // Configure introspection with client credentials
        options.UseIntrospection()
               .SetClientId(introspectionClient.ClientId)
               .SetClientSecret(introspectionClient.ClientSecret);

        //options.AddEncryptionKey(new SymmetricSecurityKey(
        //        Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        options.UseSystemNetHttp();

        options.UseAspNetCore();
    });

// Add authentication and authorization
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
// Internal endpoints may only be called by backend services, not interactive users. The rule is the
// framework's, not a second copy of it: Tenancy exposes an internal surface too (#84), and two copies of an
// authorization policy is how one of them quietly ends up weaker than the other.
builder.Services.AddAuthorization(options =>
    options.AddInternalServicePolicy(backendClient.ClientId));
builder.Services.AddPermissionAuthorization();

var app = builder.Build();

app.Services.UseDomainServices();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapDefaultEndpoints();

app.MapEndpoints();
app.MapInternalEndpoints();

using (var scope = app.Services.CreateScope())
{
    var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();

    // Set a default tenant for seeding (you might want to handle this differently)
    using var tenantChange = currentTenant.Change(new TenantInfo(Guid.Empty, "Host")); // System-wide roles

    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking for pending migrations...");

        // Check if there are pending migrations
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingList = pendingMigrations.ToList();

        logger.LogInformation("Found {Count} pending migrations", pendingList.Count);

        if (pendingList.Any())
        {
            foreach (var migration in pendingList)
            {
                logger.LogInformation("Pending migration: {Migration}", migration);
            }

            logger.LogInformation("Applying migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations found");

            // Check if tables exist
            var tablesExist = await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'") > 0;

            logger.LogInformation("__EFMigrationsHistory table exists: {Exists}", tablesExist);
        }

        // Seed data
        await SeedRoles(scope.ServiceProvider);
        await SeedUsers(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        throw; // This will prevent the app from starting if migration fails
    }
}

app.Run();

static async Task SeedRoles(IServiceProvider services)
{
    var roleDefinitions = services.GetRequiredService<IRoleDefinitionProvider>();

    // Host (system-wide) roles.
    foreach (var definition in roleDefinitions.GetAll())
    {
        await EnsureRoleAsync(services, definition, Guid.Empty);
    }

    // Tenant-scoped copies of the static roles for the seeded tenant, so that role lookups
    // (tenant-filtered at request time) resolve for that tenant — e.g. a host admin can
    // assign "Admin" to a tenant user and it will resolve within the tenant.
    foreach (var definition in roleDefinitions.GetAll())
    {
        await EnsureRoleAsync(services, definition, DemoSeed.TenantId);
    }
}

/// <summary>
/// Ensures a role exists in the given tenant and seeds its permission claims idempotently.
/// The role definition (name + default permission set) comes from the
/// <see cref="IRoleDefinitionProvider"/>. Runs on every startup, so the default roles keep
/// their standard permission set — deliberate "defaults reset" semantics for system roles.
/// </summary>
static async Task EnsureRoleAsync(IServiceProvider services, RoleDefinition definition, Guid tenantId)
{
    var currentTenant = services.GetRequiredService<ICurrentTenant>();
    var roleManager = services.GetRequiredService<RoleManager<Role>>();
    var permissionCatalog = services.GetRequiredService<IPermissionDefinitionProvider>();

    using (currentTenant.Change(new TenantInfo(tenantId, "Seed")))
    {
        // Roles are only provisioned on the sides they are available on: a host-only
        // role definition is not created in tenant tenants and vice versa.
        var roleSide = tenantId == Guid.Empty
            ? PermissionMultiTenancySide.Host
            : PermissionMultiTenancySide.Tenant;

        if (!definition.Side.IsAvailableOn(roleSide))
        {
            return;
        }

        var role = await roleManager.FindByNameAsync(definition.Name);
        if (role is null)
        {
            role = Role.Create(
                Guid.NewGuid(),
                definition.Name,
                tenantId,
                definition.Description,
                definition.Side,
                isStatic: definition.IsStatic
            );
            await roleManager.CreateAsync(role);
        }
        else if (definition.IsStatic && !role.IsStatic)
        {
            // Roles that predate the IsStatic column. Seeding is the only place that knows a role
            // came from a definition, so it corrects the flag here rather than relying on a data fix.
            role.MarkAsStatic();
            await roleManager.UpdateAsync(role);
        }

        // Permissions are only grantable on the side they are available on: Admin
        // (DefaultPermissions == null) is granted every permission available on this
        // role's side; other roles get their declared defaults filtered by side too.

        var toGrant = definition.GrantsAllPermissions
            ? permissionCatalog.GetAll()
                .Where(p => p.MultiTenancySide.IsAvailableOn(roleSide))
                .Select(p => p.Name.Value)
            : definition.DefaultPermissions!
                .Where(name => permissionCatalog.GetAll().Any(p =>
                    p.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)
                    && p.MultiTenancySide.IsAvailableOn(roleSide)))
                .Select(p => p.Value);

        // Seed permission claims idempotently (permissions are stored as claims).
        var existing = (await roleManager.GetClaimsAsync(role))
            .Select(c => c.Type)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in toGrant)
        {
            if (existing.Contains(permission))
            {
                continue;
            }

            await roleManager.AddClaimAsync(role, new Claim(permission, "true"));
        }
    }
}

static async Task SeedUsers(IServiceProvider services)
{
    var currentTenant = services.GetRequiredService<ICurrentTenant>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = new PasswordHasher<User>();
    const string password = "P@SSw0rd";

    var users = new[]
    {
        (Email: "mock@user.com", Name: "Mock User", TenantId: Guid.Empty, ConfirmEmail: true, Role: "Admin"),
        (Email: "tenant@user.com", Name: "Tenant User", TenantId: DemoSeed.TenantId, ConfirmEmail: true, Role: "Manager"),
        // The demo tenant's administrator. The seeded catalog's permissions (catalog.view /
        // catalog.write, #78) are granted to Admin only, so this is the account that can both sign
        // in and see the portal's Catalog section. TenantDataSeeder creates one of these when a
        // tenant is provisioned through the API, but the demo tenant predates that path (#93).
        (Email: "tenantadmin@user.com", Name: "Tenant Administrator", TenantId: DemoSeed.TenantId, ConfirmEmail: true, Role: "Admin"),
        // Left unconfirmed on purpose to exercise the email confirmation (OTP) flow on login.
        (Email: "unconfirmed@user.com", Name: "Unconfirmed User", TenantId: Guid.Empty, ConfirmEmail: false, Role: "User")
    };

    foreach (var (email, _, tenantId, confirmEmail, roleName) in users)
    {
        // Scope user/role lookups to the user's tenant so role resolution works
        // (user and role queries are tenant-filtered at request time).
        using (currentTenant.Change(new TenantInfo(tenantId, "Seed")))
        {
            // Look up by email first; fall back to the username to find users seeded
            // earlier when the email/username arguments were swapped (their email field
            // held the display name, so an email lookup misses them).
            var user = await userManager.FindByEmailAsync(email)
                ?? await userManager.FindByNameAsync(email);

            if (user is null)
            {
                user = User.Create(
                    Guid.NewGuid(),
                    email, // Email
                    email, // UserName (login identifier — emails are used as usernames)
                    tenantId);

                var hashedPassword = passwordHasher.HashPassword(user, password);

                user.SetPassword(hashedPassword);
                if (confirmEmail)
                {
                    user.ConfirmEmail();
                }

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed user '{email}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                // Repair users seeded with swapped email/username (email held the display
                // name). Kept EmailConfirmed as-is so confirmation state is preserved.
                user.Email = email;
                user.NormalizedEmail = email.ToUpperInvariant();

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to repair seeded user '{email}': {string.Join("; ", updateResult.Errors.Select(e => e.Description))}");
                }
            }

            // Role names are tenant-scoped and the ambient query filter is bypassed at
            // host scope (Guid.Empty), so resolve the role explicitly within this tenant.
            var role = await dbContext.Roles.IgnoreQueryFilters()
                .SingleOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant() && r.TenantId == tenantId);

            if (role is null)
            {
                throw new InvalidOperationException($"Default role '{roleName}' was not seeded for tenant {tenantId}.");
            }

            // Drop stale joins to same-named roles in other tenants (data seeded before
            // role names were tenant-scoped points at the old global copies).
            var staleJoins = await (
                from ur in dbContext.UserRoles.IgnoreQueryFilters()
                join r in dbContext.Roles.IgnoreQueryFilters() on ur.RoleId equals r.Id
                where ur.UserId == user.Id
                    && r.NormalizedName == role.NormalizedName
                    && r.Id != role.Id
                select ur).ToListAsync();

            if (staleJoins.Count > 0)
            {
                dbContext.UserRoles.RemoveRange(staleJoins);
            }

            // Assign the default role idempotently (also covers pre-existing users that
            // were seeded before roles existed).
            if (!await dbContext.UserRoles.IgnoreQueryFilters().AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id))
            {
                dbContext.UserRoles.Add(UserRole.Create(user, role));
            }

            await dbContext.SaveChangesAsync();
        }
    }
}

/// <summary>
/// The fixed data the demo tenant is seeded into, and the development accounts seeded on startup.
/// </summary>
/// <remarks>
/// <para>
/// The tenant id is a constant because three seeding steps must agree on it: the tenant's roles (see
/// <c>SeedRoles</c>), its users (see <c>SeedUsers</c>), and the sample catalog <c>CatalogDataSeeder</c> seeds
/// in the Catalog service, which carries the same id. A second literal is how they drift.
/// </para>
/// <para>
/// <strong>Development credentials</strong> — all accounts use the password <c>P@SSw0rd</c>:
/// </para>
/// <list type="table">
///   <listheader><term>Account</term><description>What it can do</description></listheader>
///   <item>
///     <term><c>mock@user.com</c> (host Admin)</term>
///     <description>Everything, at host scope — the account most manual checks use.</description>
///   </item>
///   <item>
///     <term><c>tenantadmin@user.com</c> (Admin of the demo tenant)</term>
///     <description>
///       The demo tenant's administrator. This is the account to use for the M1 acceptance criterion
///       "a tenant administrator can create and see their catalog in the portal": it signs in as a
///       tenant user and holds <c>catalog.view</c> / <c>catalog.write</c>.
///     </description>
///   </item>
///   <item>
///     <term><c>tenant@user.com</c> (Manager of the demo tenant)</term>
///     <description>
///       A tenant <em>non</em>-admin: holds the Manager role's grants only, so it is the account that
///       shows whether role-derived permissions reach a tenant user through the internal permission
///       endpoint.
///     </description>
///   </item>
///   <item>
///     <term><c>unconfirmed@user.com</c> (host User, unconfirmed)</term>
///     <description>Exercises the email confirmation (OTP) flow.</description>
///   </item>
/// </list>
/// </remarks>
static class DemoSeed
{
    /// <summary>The tenant the demo administrator, the demo manager and the sample catalog belong to.</summary>
    public static readonly Guid TenantId = new("B5D0C0E4-3A5B-4CDC-8D2A-7F1F6C9F5B4E");
}
