using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Infrastructure;
using ShopIt.Framework.Infrastructure.Security;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Presentation;
using ShopIt.Identity.Events;
using ShopIt.Tenancy.Application;
using ShopIt.Tenancy.Domain.Permissions;
using ShopIt.Tenancy.Infrastructure;
using ShopIt.Tenancy.Persistence;
using ShopIt.Tenancy.Persistence.Data;
using ShopIt.Tenancy.Presentation;
using ShopIt.Tenancy.Presentation.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddDomainServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddApplication();

// Publish-only: Tenancy emits tenant lifecycle and permission-catalog events, but consumes none.
// Stated explicitly so it cannot be confused with a service whose inbox was never configured.
builder.Services.AddPersistence("tenancy-db", builder.Configuration, InboxSubscription.None);
builder.EnrichNpgsqlDbContext<TenancyDbContext>();

// This service's permissions are published to Identity through the framework hook: at startup
// the provider's groups go to the outbox as a PermissionCatalogPublishedIntegrationEvent, so
// Identity persists them and grants new permissions to the Admin role without being redeployed.
builder.Services.AddPermissionCatalogPublishing<ShopItTenancyPermissionDefinitionProvider, TenancyDbContext>(
    ShopItTenancyPermissionDefinitionProvider.SourceService,
    PermissionCatalogPublishedIntegrationEvent.Create);

// Validate access tokens issued by the authentication server (introspection) and resolve
// the caller's permissions from the Identity service. The introspection secret is injected by the
// AppHost; a missing one fails here rather than as an unexplained 401 on every request.
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
builder.Services.AddTenantPermissionAuthorization();
builder.Services.AddTenancyInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();


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
    var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking for pending tenancy migrations...");
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingList = pendingMigrations.ToList();

        logger.LogInformation("Found {Count} pending tenancy migrations", pendingList.Count);

        if (pendingList.Any())
        {
            logger.LogInformation("Applying tenancy migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Tenancy migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending tenancy migrations found");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the tenancy database");
        throw;
    }
}

app.Run();
