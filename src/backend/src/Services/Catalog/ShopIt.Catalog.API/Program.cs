using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using ShopIt.Catalog.Application;
using ShopIt.Catalog.Domain.Permissions;
using ShopIt.Catalog.Infrastructure;
using ShopIt.Catalog.Persistence;
using ShopIt.Catalog.Persistence.Data;
using ShopIt.Catalog.Presentation;
using ShopIt.Catalog.Presentation.Authorization;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Infrastructure;
using ShopIt.Framework.Infrastructure.Security;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Presentation;
using ShopIt.Identity.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddDomainServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddApplication();

// The interim currency source (#84). ADR-0005 puts the currency on the tenant, but nothing exposes a
// tenant's currency to this service yet, so one platform-wide setting stands in: every price is stamped
// with it and a request naming another currency is refused. Bound here, in the composition root, because
// it is deployment configuration rather than a domain rule.
builder.Services.AddOptions<CatalogOptions>()
    .Bind(builder.Configuration.GetSection(CatalogOptions.SectionName));

// Publish-only: the catalog service publishes its permission catalog and will publish catalog domain
// events, but consumes nothing yet. Stated explicitly so it cannot be confused with a service whose
// inbox was never configured.
builder.Services.AddPersistence("catalog-db", builder.Configuration, InboxSubscription.None);
builder.EnrichNpgsqlDbContext<CatalogDbContext>();

// This service's permissions are published to Identity through the framework hook: at startup the
// provider's groups go to the outbox as a PermissionCatalogPublishedIntegrationEvent, so Identity
// persists them and grants new permissions to the Admin role without being redeployed. This is the
// real owner of catalog.view / catalog.write — the #24 placeholder in Notifications is gone.
builder.Services.AddPermissionCatalogPublishing<ShopItCatalogPermissionDefinitionProvider, CatalogDbContext>(
    ShopItCatalogPermissionDefinitionProvider.SourceService,
    PermissionCatalogPublishedIntegrationEvent.Create);

// Validate access tokens issued by the authentication server (introspection) and resolve the caller's
// permissions from the Identity service. The introspection secret is injected by the AppHost; a missing
// one fails here rather than as an unexplained 401 on every request.
//
// Enforcing catalog.view / catalog.write means resolving a permission over HTTP, which is why this
// service now has an Infrastructure project and a ShopIt.Identity.Client reference: declaring a
// permission and enforcing it are different dependencies. See docs/CONTRIBUTING.md's per-service table.
var introspectionClient = ServiceClientCredentials.Read(
    builder.Configuration, "InternalClients:Introspection", "token introspection");

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("https://localhost:7234/");

        options.UseIntrospection()
               .SetClientId(introspectionClient.ClientId)
               .SetClientSecret(introspectionClient.ClientSecret);

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();
builder.Services.AddCatalogPermissionAuthorization();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

var app = builder.Build();

app.Services.UseDomainServices();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking for pending catalog migrations...");
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingList = pendingMigrations.ToList();

        logger.LogInformation("Found {Count} pending catalog migrations", pendingList.Count);

        if (pendingList.Any())
        {
            logger.LogInformation("Applying catalog migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Catalog migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending catalog migrations found");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the catalog database");
        throw;
    }

    // The portal needs a catalog to show on a fresh database. The seeder is idempotent and enters the
    // seeded tenant itself — never host scope, where the tenant filter is off and rows would have no owner.
    try
    {
        var catalogOptions = scope.ServiceProvider.GetRequiredService<IOptions<CatalogOptions>>().Value;
        var seeder = scope.ServiceProvider.GetRequiredService<CatalogDataSeeder>();

        await seeder.SeedAsync(catalogOptions.NormalizedCurrency);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the catalog database");
        throw;
    }
}

app.Run();
