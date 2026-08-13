using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Infrastructure;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Notifications.Application;
using ShopIt.Notifications.Application.Contracts.Events;
using ShopIt.Notifications.Persistence;
using ShopIt.Notifications.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddDomainServices();

// Consume the generic notification event published by the Identity and Authentication
// services and hand every message to the configured email sender.
builder.Services.AddPersistence(
    "notifications-db",
    builder.Configuration,
    configureInbox: inbox => inbox.Topics.Add(nameof(SendEmailIntegrationEvent)),
    handlerAssemblies: typeof(ShopIt.Notifications.Application.DependencyInjection).Assembly);
builder.EnrichNpgsqlDbContext<NotificationsDbContext>();

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
