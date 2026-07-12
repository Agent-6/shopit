using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using Refit;
using ShopIt.Framework.Presentation;
using ShopIt.Identity.Application;
using ShopIt.Identity.Application.Contracts.Clients;
using ShopIt.Identity.Application.Tenancy;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;
using ShopIt.Identity.Persistence;
using ShopIt.Identity.Persistence.Data;
using ShopIt.Identity.Presentation;
using ShopIt.Identity.Presentation.Internal;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

// request handlers
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddPersistence("identity-db", builder.Configuration);
// TODO: move this to the persistence extension method,and make it an extension method on WebApplicationBuilder
builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

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
               .SetClientId("identity-api")      // The client you created
               .SetClientSecret("SECRET");  // Same secret as above

        //options.AddEncryptionKey(new SymmetricSecurityKey(
        //        Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

        options.UseSystemNetHttp();

        options.UseAspNetCore();
    });

// Add authentication and authorization
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();

var app = builder.Build();

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
    var roleManager = services.GetRequiredService<RoleManager<Role>>();

    string[] roles = { "Admin", "User", "Manager" };
    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = Role.Create(
                Guid.NewGuid(),
                roleName,
                Guid.Empty, // System-wide role
                "system"
            );
            await roleManager.CreateAsync(role);
        }
    }
}

