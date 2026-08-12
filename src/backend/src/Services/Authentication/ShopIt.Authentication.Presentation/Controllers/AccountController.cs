using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMockEmailService _mockEmailService;
    private readonly IFlowStatusStore _flowStatusStore;

    public AccountController(
        IIdentityServiceClient identityServiceClient,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork,
        IMockEmailService mockEmailService,
        IFlowStatusStore flowStatusStore)
    {
        _identityServiceClient = identityServiceClient;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
        _mockEmailService = mockEmailService;
        _flowStatusStore = flowStatusStore;
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

        if (!validationResult.EmailConfirmed)
        {
            return RedirectToAction("ConfirmEmail", new { email = validationResult.Email, returnUrl });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, validationResult.UserId.ToString()),
            new("tenant_id", validationResult.TenantId.ToString()),
            new(ClaimTypes.Name, validationResult.UserName),
            new(ClaimTypes.Email, validationResult.Email),
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
