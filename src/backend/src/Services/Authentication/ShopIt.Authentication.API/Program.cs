using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ShopIt.Authentication.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
//builder.Services.AddControllers();

// Add DbContext (replace with your actual DbContext)
builder.AddNpgsqlDbContext<ApplicationDbContext>("auth-db");

// Add cookie authentication for SSO session
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "sso_cookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = null;
        options.LogoutPath = null;
    });

builder.Services.AddAuthorization();

// Add OpenIddict
builder.Services.AddOpenIddict()
    // Core services (storage)
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>()
               .ReplaceDefaultEntities<Guid>();
    })
    // Server components (token issuance)
    .AddServer(options =>
    {
        // Endpoint URLs
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetUserInfoEndpointUris("connect/userinfo")
               .SetEndSessionEndpointUris("connect/logout")
               .SetIntrospectionEndpointUris("connect/introspect")
               .SetRevocationEndpointUris("connect/revoke");

        // Flows
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange()
               .AllowRefreshTokenFlow()
               .AllowClientCredentialsFlow();

        // Scopes
        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess);

        // Token lifetimes
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
               .SetRefreshTokenLifetime(TimeSpan.FromDays(14))
               .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(5));

        // Security keys (development only – replace in production)
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // ASP.NET Core integration
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough()
               .EnableStatusCodePagesIntegration();
    })
    // Validation component (for token introspection)
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHttpClient(); // for calling Identity Service
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

//app.MapControllers();
app.MapPost("/connect/token", async (HttpContext context) =>
{
    // Let OpenIddict handle the request
    var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    if (!result.Succeeded)
    {
        return Results.Challenge(properties: null,
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
    }

    // If authentication succeeded, OpenIddict has already validated the request
    // and will generate the appropriate token response.
    return Results.SignIn(result.Principal,
        new AuthenticationProperties(),
        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapGet("/connect/authorize", async (HttpContext context) =>
{
    var request = context.GetOpenIddictServerRequest();
    if (request == null)
    {
        return Results.BadRequest();
    }

    // Check if the user is already authenticated (has an SSO cookie)
    if (!context.User.Identity?.IsAuthenticated == true)
    {
        // Tell the client to show its login page
        return Results.Unauthorized();
    }

    // Create an OpenIddict principal for the authenticated user
    var identity = new ClaimsIdentity(
        authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        nameType: ClaimTypes.Name,
        roleType: ClaimTypes.Role);

    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrEmpty(userId))
    {
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
    }

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(request.GetScopes());

    // Sign in and complete the authorization request
    return Results.SignIn(principal,
        new AuthenticationProperties(),
        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapPost("/api/auth/login", async (HttpContext context, LoginRequest request,
    [FromServices] IHttpClientFactory httpClientFactory,
    [FromServices] IConfiguration config) =>
{
    // Call Identity Service to validate credentials
    var client = httpClientFactory.CreateClient("IdentityService");
    client.DefaultRequestHeaders.Add("X-API-Key", config["ApiKeys:IdentityService"]);
    var response = await client.PostAsJsonAsync("api/internal/validate-credentials", new
    {
        Username = request.Email,
        Password = request.Password
    });

    if (!response.IsSuccessStatusCode)
    {
        return Results.Unauthorized();
    }

    var validationResult = await response.Content.ReadFromJsonAsync<CredentialValidationResult>();

    // Create claims and sign in to establish SSO cookie
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, validationResult?.UserId.ToString()!),
        new(ClaimTypes.Name, validationResult?.UserName!),
        new(ClaimTypes.Email, validationResult?.Email!),
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    // Return redirect URL to complete OIDC flow
    var redirectUrl = $"{context.Request.Scheme}://{context.Request.Host}/connect/authorize{context.Request.QueryString}";
    return Results.Ok(new { message = "Login successful", redirectUrl });
});

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
        await SeedOpenIddictApplicationsAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        throw; // This will prevent the app from starting if migration fails
    }
}

app.Run();

async Task SeedOpenIddictApplicationsAsync(IServiceProvider serviceProvider)
{
    var applicationManager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    // Angular SPA client
    const string clientId = "angular-spa";
    if (await applicationManager.FindByClientIdAsync(clientId) == null)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = "Angular SPA Application",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            RedirectUris = { new Uri("https://localhost:4200/auth-callback") },
            PostLogoutRedirectUris = { new Uri("https://localhost:4200") },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
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
}


public record LoginRequest(string Email, string Password);
public record CredentialValidationResult(Guid UserId, string UserName, string Email);
