using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Infrastructure;
using ShopIt.Framework.Presentation;
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
builder.Services.AddPersistence("tenancy-db", builder.Configuration);
builder.EnrichNpgsqlDbContext<TenancyDbContext>();

// Validate access tokens issued by the authentication server (introspection) and resolve
// the caller's permissions from the Identity service.
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer("https://localhost:7234/");

        options.UseIntrospection()
               .SetClientId("identity-api")
               .SetClientSecret("SECRET");

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

// Publish this service's permission catalog so the Identity service can persist it and
// grant any new permissions to the Admin role. Runs on every startup (permission
// definitions only change when this service is redeployed), so Identity does not need to
// be redeployed when Tenancy's permissions change.
using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider.GetRequiredService<ShopItTenancyPermissionDefinitionProvider>();
    var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
    var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

    var groups = provider.GetGroups()
        .Select(g => new PermissionGroupDto(
            g.Name,
            g.DisplayName,
            g.Permissions
                .Select(p => new PermissionDefinitionDto(p.Name, p.DisplayName, p.Description, p.MultiTenancySide))
                .ToList()))
        .ToList();

    await outboxWriter.WriteAsync(new PermissionCatalogPublishedIntegrationEvent(
        ShopItTenancyPermissionDefinitionProvider.SourceService,
        groups));

    await dbContext.SaveChangesAsync();
}

app.Run();
