using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using ShopIt.Authentication.Infrastructure;
using ShopIt.Authentication.Persistence;
using ShopIt.Authentication.Persistence.Data;
using ShopIt.Framework.Domain;
using ShopIt.Framework.Persistence.Inbox;
using ShopIt.Framework.Persistence.Outbox;
using ShopIt.Identity.Application.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext and Kafka-based integration event infrastructure
builder.Services.AddPersistence(
    "auth-db",
    builder.Configuration,
    configureInbox: inbox => inbox.Topics.AddRange(new[]
    {
        nameof(PasswordResetTokenGeneratedIntegrationEvent),
        nameof(PasswordResetCompletedIntegrationEvent),
        nameof(EmailConfirmationOtpGeneratedIntegrationEvent),
        nameof(UserEmailConfirmedIntegrationEvent),
        nameof(UserInvitedIntegrationEvent),
    }),
    handlerAssemblies: typeof(ShopIt.Authentication.Infrastructure.DependencyInjection).Assembly);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://oidcdebugger.com", "http://localhost:4200", "http://localhost:4201")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var app = builder.Build();

app.Services.UseDomainServices();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
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
        await SeedScopesAsync(scope.ServiceProvider);
        await SeedOpenIddictApplicationsAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        throw; // This will prevent the app from starting if migration fails
    }
}

app.Run();

async Task SeedScopesAsync(IServiceProvider serviceProvider)
{
    var scopeManager = serviceProvider.GetRequiredService<IOpenIddictScopeManager>();
    var existingScope = await scopeManager.FindByNameAsync("shopit-api");
    if (existingScope != null)
    {
        await scopeManager.DeleteAsync(existingScope);
    }
    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
    {
        Name = "shopit-api",
        DisplayName = "ShopIt API Access",
        Resources =
        {
            "identity-api"
        }
    });
}

async Task SeedOpenIddictApplicationsAsync(IServiceProvider serviceProvider)
{
    var applicationManager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    // Angular SPA client
    const string clientId = "angular-spa";
    var existingApp = await applicationManager.FindByClientIdAsync(clientId);
    if (existingApp != null)
    {
        await applicationManager.DeleteAsync(existingApp);
    }
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Angular SPA Application",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            RedirectUris = { new Uri("http://localhost:4200/auth-callback"), new Uri("http://localhost:4201/auth-callback") },
            PostLogoutRedirectUris = { 
                new Uri("http://localhost:4200"),
                new Uri("http://localhost:4201"),
                new Uri("http://localhost:4200/auth-callback"),
                new Uri("http://localhost:4201/auth-callback")
            },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}shopit-api",
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };
        await applicationManager.CreateAsync(descriptor);
    }

    const string debugClientId = "oidc-debugger";
    var existingDebugApp = await applicationManager.FindByClientIdAsync(debugClientId);
    if (existingDebugApp != null)
    {
        await applicationManager.DeleteAsync(existingDebugApp);
    }
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = debugClientId,
            DisplayName = "OIDC Debugger",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            RedirectUris = { new Uri("https://oidcdebugger.com/debug") },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };
        await applicationManager.CreateAsync(descriptor);
    }

    const string identityApiClientId = "identity-api";
    var existingApiClient = await applicationManager.FindByClientIdAsync(identityApiClientId);
    if (existingApiClient != null)
    {
        await applicationManager.DeleteAsync(existingApiClient);
    }

    await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
    {
        ClientId = identityApiClientId,
        ClientSecret = "SECRET",
        DisplayName = "Identity API Resource Server",
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Introspection,
        }
    });

    // Backend-to-backend client credentials client
    const string backendClientId = "shopit-backend";
    var existingBackendApp = await applicationManager.FindByClientIdAsync(backendClientId);
    if (existingBackendApp != null)
    {
        await applicationManager.DeleteAsync(existingBackendApp);
    }

    await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
    {
        ClientId = backendClientId,
        ClientSecret = "BACKEND_SECRET",
        DisplayName = "ShopIt Backend Service",
        ClientType = OpenIddictConstants.ClientTypes.Confidential,
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.Revocation,
            OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
            $"{OpenIddictConstants.Permissions.Prefixes.Scope}shopit-api",
        }
    });
}

