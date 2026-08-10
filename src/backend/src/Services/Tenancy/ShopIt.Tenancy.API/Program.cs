using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Infrastructure;
using ShopIt.Framework.Presentation;
using ShopIt.Tenancy.Application;
using ShopIt.Tenancy.Persistence;
using ShopIt.Tenancy.Persistence.Data;
using ShopIt.Tenancy.Presentation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.Services.AddDomainServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPersistence("tenancy-db", builder.Configuration);
builder.EnrichNpgsqlDbContext<TenancyDbContext>();


var app = builder.Build();

app.Services.UseDomainServices();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

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
