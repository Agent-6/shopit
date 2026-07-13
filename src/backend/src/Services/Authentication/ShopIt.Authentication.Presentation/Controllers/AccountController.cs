using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ShopIt.Identity.Application.Contracts.Models;
using ShopIt.Identity.Application.Contracts.Services;

namespace ShopIt.Authentication.Presentation.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly IIdentityServiceClient _identityServiceClient;

    public AccountController(IIdentityServiceClient identityServiceClient)
    {
        _identityServiceClient = identityServiceClient;
    }

    [HttpGet("Login")]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginRequest request, [FromQuery] string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        var validationResult = await _identityServiceClient.ValidateCredentialsAsync(
            new CredentialValidationRequest(request.Email, request.Password));

        if (validationResult == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(request);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, validationResult?.UserId.ToString()!),
            new("tenant_id", validationResult?.TenantId.ToString()!),
            new(ClaimTypes.Name, validationResult?.UserName!),
            new(ClaimTypes.Email, validationResult?.Email!),
        };
        
        var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var currentPrincipal = HttpContext.User;
        ClaimsPrincipal newPrincipal;

        if (currentPrincipal?.Identity?.IsAuthenticated == true)
        {
            var identities = currentPrincipal.Identities.ToList();
            identities.RemoveAll(i => i.FindFirst(ClaimTypes.Email)?.Value == validationResult.Email);
            identities.Insert(0, newIdentity);
            newPrincipal = new ClaimsPrincipal(identities);
        }
        else
        {
            newPrincipal = new ClaimsPrincipal(newIdentity);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("~/");
    }

    [HttpGet("Switcher")]
    public IActionResult Switcher(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        var currentPrincipal = HttpContext.User;
        if (currentPrincipal?.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", new { returnUrl });
        }

        var identities = currentPrincipal.Identities.Where(i => i.IsAuthenticated).ToList();
        return View(identities);
    }

    [HttpPost("SwitchAccount")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchAccount(string email, string returnUrl = null)
    {
        var currentPrincipal = HttpContext.User;
        if (currentPrincipal?.Identity?.IsAuthenticated == true)
        {
            var identities = currentPrincipal.Identities.ToList();
            var targetIdentity = identities.FirstOrDefault(i => i.FindFirst(ClaimTypes.Email)?.Value == email);

            if (targetIdentity != null)
            {
                identities.Remove(targetIdentity);
                identities.Insert(0, targetIdentity);
                
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identities));
            }
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return Redirect("~/");
    }

    [HttpGet("Logout")]
    public IActionResult Logout(string returnId = null)
    {
        return View();
    }

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutConfirm()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("~/");
    }

    [HttpGet("ForgotPassword")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("ForgotPassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword([FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Email is required.");
            return View();
        }

        await _identityServiceClient.ForgotPasswordAsync(email);
        return View("ForgotPasswordConfirmation");
    }

    [HttpGet("ResetPassword")]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return BadRequest("A token and email must be supplied for password reset.");
            
        return View(new ResetPasswordRequest { Email = email, Token = token });
    }

    [HttpPost("ResetPassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var result = await _identityServiceClient.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        if (result)
            return View("ResetPasswordConfirmation");

        ModelState.AddModelError(string.Empty, "Failed to reset password. The token may be expired or invalid.");
        return View(request);
    }
}

public record LoginRequest(string Email, string Password);

public class ResetPasswordRequest
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}
