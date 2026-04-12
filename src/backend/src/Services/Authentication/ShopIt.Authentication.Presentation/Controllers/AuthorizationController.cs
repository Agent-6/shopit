using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace ShopIt.Authentication.Presentation.Controllers;

public class AuthorizationController : Controller
{
    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            return Challenge(
                properties: null,
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
                var userName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(userName))
                {
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, userName)
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));
                }

                var principal = new ClaimsPrincipal(identity);
                principal.SetScopes(request.GetScopes());

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
