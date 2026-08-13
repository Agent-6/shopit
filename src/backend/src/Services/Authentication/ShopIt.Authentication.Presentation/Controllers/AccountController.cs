using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using ShopIt.Authentication.Application.Mocking;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Core.UnitOfWork;
using ShopIt.Identity.Application.Contracts.Events;
using ShopIt.Identity.Application.Contracts.Models;
using ShopIt.Identity.Application.Contracts.Services;

namespace ShopIt.Authentication.Presentation.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    /// <summary>
    /// One-shot cookie set by the account switcher after the user picks an account. The
    /// authorize endpoint checks it to avoid re-showing the switcher for the same round.
    /// </summary>
    public const string AccountSwitcherCookieName = "shopit_account_selected";

    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMockEmailService _mockEmailService;
    private readonly IFlowStatusStore _flowStatusStore;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public AccountController(
        IIdentityServiceClient identityServiceClient,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork,
        IMockEmailService mockEmailService,
        IFlowStatusStore flowStatusStore,
        IOpenIddictApplicationManager applicationManager)
    {
        _identityServiceClient = identityServiceClient;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
        _mockEmailService = mockEmailService;
        _flowStatusStore = flowStatusStore;
        _applicationManager = applicationManager;
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

        if (validationResult is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(request);
        }

        if (!validationResult.Success)
        {
            if (validationResult.ErrorCode == "ACCOUNT_NOT_ACTIVATED")
            {
                // Phase 4 safeguard: the user exists but has not clicked their invitation
                // link yet. Point them back to their inbox and offer to resend the invite.
                ViewData["ShowResendInvitation"] = true;
                ViewData["PendingEmail"] = request.Email;
                ModelState.AddModelError(string.Empty, "Your account registration is incomplete. Please check your email for your invitation link.");
            }
            else if (validationResult.ErrorCode == "ACCOUNT_DISABLED")
            {
                ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact your administrator.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(request);
        }

        if (!validationResult.EmailConfirmed)
        {
            return RedirectToAction("ConfirmEmail", new { email = validationResult.Email, returnUrl });
        }

        await SignInAsync(
            validationResult.UserId,
            validationResult.TenantId,
            validationResult.UserName!,
            validationResult.Email!);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("~/");
    }

    // ------------------------------------------------------------------
    // Account activation (invitation flow)
    // ------------------------------------------------------------------

    [HttpGet("Activate")]
    public IActionResult Activate(string userId, string token, string clientId = null)
    {
        if (!Guid.TryParse(userId, out var parsedUserId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("A user id and activation token must be supplied.");
        }

        return View(new ActivateViewModel
        {
            UserId = parsedUserId,
            Token = token,
            ClientId = clientId ?? "angular-spa"
        });
    }

    [HttpPost("Activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate([FromForm] ActivateViewModel request)
    {
        if (!ModelState.IsValid)
            return View(request);

        if (request.NewPassword != request.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "The passwords do not match.");
            return View(request);
        }

        // Synchronous call to the Identity service: validates the token, stores the
        // password and activates the account. The browser waits for the result.
        var result = await _identityServiceClient.ActivateUserAsync(
            new ActivateUserRequest(request.UserId, request.Token, request.NewPassword));

        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "We could not reach the account service. Please try again.");
            return View(request);
        }

        if (!result.Succeeded)
        {
            if (result.ErrorCode == "ACTIVATION_TOKEN_EXPIRED")
            {
                ViewData["Email"] = result.Email;
                return View("ActivationExpired");
            }

            if (result.ErrorCode == "PASSWORD_POLICY")
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "The password does not meet the requirements.");
                return View(request);
            }

            return View("ActivationInvalid");
        }

        // Automatic single sign-on: issue the local auth cookie, then hand the browser
        // back to the SPA. The SPA's auth guard immediately starts the OIDC code flow,
        // which OpenIddict short-circuits because the cookie is present — the user lands
        // on their dashboard without a second login.
        await SignInAsync(result.UserId, result.TenantId, result.UserName, result.Email);

        var redirectUri = await ResolveSpaRedirectUriAsync(request.ClientId);
        if (!string.IsNullOrEmpty(redirectUri))
        {
            return Redirect(redirectUri);
        }

        return Redirect("~/");
    }

    [HttpPost("ResendInvitation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvitation([FromForm] string email, [FromQuery] string returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction("Login", new { returnUrl });
        }

        // The Identity service issues a fresh activation token and re-publishes
        // UserInvitedIntegrationEvent, which delivers a new invitation email.
        var requestId = Guid.NewGuid();
        await PublishAsync(new ResendInvitationRequestedIntegrationEvent(requestId, email.Trim()), HttpContext.RequestAborted);

        ViewData["Email"] = email.Trim();
        return View("ResendInvitationConfirmation");
    }

    // ------------------------------------------------------------------
    // Account switcher
    // ------------------------------------------------------------------

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

                // Mark this authorize round as "already chosen" so the authorize endpoint
                // does not bounce straight back into the switcher after the redirect.
                HttpContext.Response.Cookies.Append(AccountSwitcherCookieName, "1", new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = true,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromMinutes(5),
                });
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

    // ------------------------------------------------------------------
    // Forgot password (event-driven)
    // ------------------------------------------------------------------

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

        // The Identity service generates the reset token and replies asynchronously
        // with PasswordResetTokenGeneratedIntegrationEvent, which lands in the mock inbox.
        var requestId = Guid.NewGuid();
        await PublishAsync(new ForgotPasswordRequestedIntegrationEvent(requestId, email.Trim()), HttpContext.RequestAborted);

        ViewData["Email"] = email.Trim();
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

        if (request.NewPassword != request.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "The passwords do not match.");
            return View(request);
        }

        // The Identity service applies the reset and replies asynchronously with
        // PasswordResetCompletedIntegrationEvent; this page polls for the outcome.
        var requestId = Guid.NewGuid();
        await PublishAsync(
            new PasswordResetRequestedIntegrationEvent(requestId, request.Email, request.Token, request.NewPassword),
            HttpContext.RequestAborted);

        ViewData["RequestId"] = requestId;
        ViewData["Email"] = request.Email;
        ViewData["Token"] = request.Token;
        return View("ResetPasswordProcessing");
    }

    // ------------------------------------------------------------------
    // Email confirmation (event-driven)
    // ------------------------------------------------------------------

    [HttpGet("ConfirmEmail")]
    public IActionResult ConfirmEmail(string email, string returnUrl = null)
    {
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", new { returnUrl });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new ConfirmEmailViewModel { Email = email });
    }

    [HttpPost("ConfirmEmail/SendOtp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmailConfirmationOtp([FromForm] string email, [FromQuery] string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Email is required.");
            return View("ConfirmEmail", new ConfirmEmailViewModel { Email = email });
        }

        // The Identity service generates the OTP and replies asynchronously with
        // EmailConfirmationOtpGeneratedIntegrationEvent, which lands in the mock inbox.
        var requestId = Guid.NewGuid();
        await PublishAsync(new EmailConfirmationOtpRequestedIntegrationEvent(requestId, email.Trim()), HttpContext.RequestAborted);

        ViewData["OtpSent"] = true;
        return View("ConfirmEmail", new ConfirmEmailViewModel { Email = email.Trim() });
    }

    [HttpPost("ConfirmEmail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail([FromForm] ConfirmEmailViewModel request, [FromQuery] string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        // The Identity service validates the code and replies asynchronously with
        // UserEmailConfirmedIntegrationEvent; this page polls for the outcome.
        var requestId = Guid.NewGuid();
        await PublishAsync(
            new EmailConfirmationSubmittedIntegrationEvent(requestId, request.Email, request.Code),
            HttpContext.RequestAborted);

        ViewData["RequestId"] = requestId;
        return View("ConfirmEmailProcessing");
    }

    // ------------------------------------------------------------------
    // Polling endpoints used by the event-driven views
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns the mock inbox for an address so views can display delivered emails.
    /// </summary>
    [HttpGet("MockEmails")]
    public IActionResult MockEmails(string email)
    {
        return Json(_mockEmailService.GetInbox(email));
    }

    /// <summary>
    /// Returns the outcome of an asynchronous flow (or null while still pending).
    /// </summary>
    [HttpGet("FlowStatus")]
    public IActionResult FlowStatus(Guid requestId)
    {
        return Json(_flowStatusStore.Get(requestId));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Issues the local SSO cookie, adding a second identity when the user is already
    /// signed in with a different account (multi-account / account switcher support).
    /// </summary>
    private async Task SignInAsync(Guid userId, Guid tenantId, string userName, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, email),
        };

        var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var currentPrincipal = HttpContext.User;
        ClaimsPrincipal newPrincipal;

        if (currentPrincipal?.Identity?.IsAuthenticated == true)
        {
            var identities = currentPrincipal.Identities.ToList();
            identities.RemoveAll(i => i.FindFirst(ClaimTypes.Email)?.Value == email);
            identities.Insert(0, newIdentity);
            newPrincipal = new ClaimsPrincipal(identities);
        }
        else
        {
            newPrincipal = new ClaimsPrincipal(newIdentity);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);
    }

    /// <summary>
    /// Resolves the SPA's registered redirect URI for the given OpenIddict client id, so
    /// the activation flow can hand the signed-in browser back to the client application.
    /// </summary>
    private async Task<string?> ResolveSpaRedirectUriAsync(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var application = await _applicationManager.FindByClientIdAsync(clientId);
        if (application is null)
            return null;

        var redirectUris = await _applicationManager.GetRedirectUrisAsync(application);
        return redirectUris.FirstOrDefault();
    }

    /// <summary>
    /// Enqueues an integration event into the transactional outbox. The event is
    /// committed atomically with the request's transaction and published to Kafka
    /// by the background <see cref="ShopIt.Framework.Persistence.Outbox.OutboxProcessor{TContext}"/>.
    /// </summary>
    private async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _outboxWriter.WriteAsync(integrationEvent, cancellationToken);
            return true;
        }, cancellationToken);
    }
}

public record LoginRequest(string Email, string Password);

public class ResetPasswordRequest
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}

public class ConfirmEmailViewModel
{
    public string Email { get; set; }
    public string Code { get; set; }
}

public class ActivateViewModel
{
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public string ClientId { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}
