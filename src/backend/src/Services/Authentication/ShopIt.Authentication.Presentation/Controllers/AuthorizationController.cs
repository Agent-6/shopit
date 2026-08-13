using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace ShopIt.Authentication.Presentation.Controllers;

public class AuthorizationController(IOpenIddictScopeManager scopeManager) : Controller
{
    private readonly IOpenIddictScopeManager _scopeManager = scopeManager;

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            return Challenge(
                properties: null,
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        // client_credentials: OpenIddict validates the client credentials but the
        // principal it returns carries no subject claim (and isn't marked authenticated).
        // Build the token principal explicitly with the client id as the subject — the
        // pattern from the official OpenIddict client_credentials sample.
        if (request.IsClientCredentialsGrantType())
        {
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(
                OpenIddictConstants.Claims.Subject,
                request.ClientId!,
                OpenIddictConstants.Destinations.AccessToken);

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            // Resolve the resources (audiences) associated with the requested scopes
            // so introspection/validation can map the token to the right API.
            var resources = await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync();
            principal.SetResources(resources);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // authorization_code / refresh_token: the handler attaches the user's subject,
        // but ASP.NET Core 9+ (RequireAuthenticatedSignIn) rejects SignInAsync with
        // unauthenticated principals, so mark the identity explicitly.
        var userPrincipal = result.Principal!;
        if (userPrincipal.Identity?.IsAuthenticated != true)
        {
            userPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(userPrincipal.Claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
        }

        return SignIn(userPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() 
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.HasPromptValue(OpenIddictConstants.PromptValues.SelectAccount))
        {
            var returnUrl = Request.PathBase + Request.Path + QueryString.Create(
                Request.Query.Where(q => q.Key != "prompt").ToList());
            
            return RedirectToAction("Switcher", "Account", new { returnUrl });
        }

        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
        {
            return Challenge(
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(Request.Query.ToList())
                },
                authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme]);
        }

        // POST request means the user submitted the consent form
        if (HttpMethods.IsPost(Request.Method))
        {
            var form = await Request.ReadFormAsync();
            if (form.ContainsKey("submit.Accept"))
            {
                var identity = new ClaimsIdentity(
                    authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    nameType: ClaimTypes.Name,
                    roleType: ClaimTypes.Role);

                var userId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, userId)
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));
                }

                // TODO: check scope for tenant id claim
                var tenantId = result.Principal.FindFirst("tenant_id")?.Value;
                if (!string.IsNullOrEmpty(tenantId))
                {
                    identity.AddClaim(new Claim("tenant_id", tenantId)
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken));
                }

                var userName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(userName))
                {
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, userName)
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));
                }

                // TODO: check scope for email claim
                var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(email))
                {
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, email)
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));
                }

                var principal = new ClaimsPrincipal(identity);
                var scopes = request.GetScopes();
                principal.SetScopes(scopes);

                // for introspection endpoint to work, we need to include the list of resources associated with the requested scopes
                // without this, custom claims won't be included in claims principal after validation, and we will not have access to tenant_id claim in the API
                var resources = await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync();
                principal.SetResources(resources);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            
            // Denied
            return Forbid(
                properties: new AuthenticationProperties()
                {
                    RedirectUri = request.RedirectUri
                },
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        // GET request: show the consent view
        return View("Consent", request);
    }
}
