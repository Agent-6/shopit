using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Infrastructure;
using ShopIt.Framework.Persistence;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Identity.Events;
using ShopIt.Notifications.Application;
using ShopIt.Notifications.Application.IntegrationEvents;
using ShopIt.Notifications.Application.Permissions;
using ShopIt.Notifications.Events;
using ShopIt.Notifications.Infrastructure;
using ShopIt.Notifications.Persistence;
using ShopIt.Notifications.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddInfrastructureServices(builder.Configuration);

// The email transport lives in this service's Infrastructure layer (SMTP today); Application decides
// what to send, this decides how.
builder.Services.AddNotificationsInfrastructure(builder.Configuration);
builder.Services.AddDomainServices();

// Consume the generic notification event published by the Identity and Authentication
// services and hand every message to the configured email sender.
builder.Services.AddPersistence(
    "notifications-db",
    builder.Configuration,
    InboxSubscription.Consuming(typeof(SendEmailIntegrationEvent)),
    handlerAssemblies: typeof(SendEmailIntegrationEventHandler).Assembly);
builder.EnrichNpgsqlDbContext<NotificationsDbContext>();

// PLACEHOLDER (#24): publishes catalog.view / catalog.write so the permission recipe - declare,
// publish through the framework hook, persist in Identity, grant to Admin, enforce with
// RequirePermission - is proven before the product catalog service exists. The probe endpoint lives
// in Tenancy.Presentation/Catalog. Delete this registration with the placeholder provider when M1
// declares the real permissions; the recipe itself needs no change.
builder.Services.AddPermissionCatalogPublishing<
    PlaceholderCatalogPermissionDefinitionProvider,
    NotificationsDbContext>(
    PlaceholderCatalogPermissionDefinitionProvider.SourceService,
    PermissionCatalogPublishedIntegrationEvent.Create);

var app = builder.Build();

app.Services.UseDomainServices();

app.MapDefaultEndpoints();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking for pending notifications migrations...");
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingList = pendingMigrations.ToList();

        logger.LogInformation("Found {Count} pending notifications migrations", pendingList.Count);

        if (pendingList.Any())
        {
            logger.LogInformation("Applying notifications migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Notifications migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending notifications migrations found");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the notifications database");
        throw;
    }
}

app.Run();
