using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using ShopIt.Identity.Application.Contracts.Models;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Presentation.Internal;

public static class InternalEndpoints
{
    private const string EmailConfirmationOtpProvider = "EmailConfirmation";
    private const string EmailConfirmationOtpName = "Otp";
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    public static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal")
            .RequireAuthorization()
            .WithTags("Internal");

        group.MapPost("/validate-credentials", ValidateCredentials);
        group.MapPost("/forgot-password", ForgotPassword);
        group.MapPost("/reset-password", ResetPassword);
        group.MapPost("/send-email-confirmation-otp", SendEmailConfirmationOtp);
        group.MapPost("/confirm-email", ConfirmEmail);

        return app;
    }

    private static async Task<IResult> ValidateCredentials(
        [FromBody] CredentialValidationRequest request,
        UserManager<User> userManager,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Username and password are required.");
        }

        var user = await userManager.FindByNameAsync(request.Username);

        if (user is null)
        {
            return Results.NotFound();
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            return Results.Unauthorized();
        }

        var result = new CredentialValidationResponse(
            user.Id,
            user.TenantId,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed);

        return Results.Ok(result);
    }

    private static async Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        UserManager<User> userManager,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest("Email is required.");
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Don't reveal whether an account exists.
            return Results.Ok(new ForgotPasswordResponse(request.Email, null));
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: replace with a real email sender.
        loggerFactory.CreateLogger("Identity.Internal")
            .LogInformation("Password reset requested for {Email}. Mock email: /Account/ResetPassword?email={Email}&token={Token}",
                user.Email, user.Email, token);

        return Results.Ok(new ForgotPasswordResponse(user.Email!, token));
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        UserManager<User> userManager,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest("Email, token and new password are required.");
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Results.BadRequest("Invalid password reset attempt.");
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        return Results.Ok(result.Succeeded);
    }

    private static async Task<IResult> SendEmailConfirmationOtp(
        [FromBody] SendEmailConfirmationOtpRequest request,
        UserManager<User> userManager,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest("Email is required.");
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Don't reveal whether an account exists.
            return Results.Ok(new SendEmailConfirmationOtpResponse(request.Email, null));
        }

        if (user.EmailConfirmed)
        {
            return Results.Ok(new SendEmailConfirmationOtpResponse(user.Email!, null));
        }

        var code = Random.Shared.Next(100000, 1000000).ToString("D6");
        var expiresAt = DateTime.UtcNow.Add(OtpLifetime);

        var result = await userManager.SetAuthenticationTokenAsync(
            user,
            EmailConfirmationOtpProvider,
            EmailConfirmationOtpName,
            $"{code}|{expiresAt:O}");

        if (!result.Succeeded)
        {
            return Results.BadRequest("Failed to generate a verification code.");
        }

        // TODO: replace with a real email sender.
        loggerFactory.CreateLogger("Identity.Internal")
            .LogInformation("Email confirmation code for {Email}: {Code} (expires at {ExpiresAt})",
                user.Email, code, expiresAt);

        return Results.Ok(new SendEmailConfirmationOtpResponse(user.Email!, code));
    }

    private static async Task<IResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        UserManager<User> userManager,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest("Email and verification code are required.");
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Results.BadRequest("Invalid email confirmation attempt.");
        }

        if (user.EmailConfirmed)
        {
            await userManager.RemoveAuthenticationTokenAsync(user, EmailConfirmationOtpProvider, EmailConfirmationOtpName);
            return Results.Ok(true);
        }

        var stored = await userManager.GetAuthenticationTokenAsync(user, EmailConfirmationOtpProvider, EmailConfirmationOtpName);
        if (string.IsNullOrEmpty(stored))
        {
            return Results.BadRequest("No verification code has been issued. Request a new code.");
        }

        var separatorIndex = stored.IndexOf('|');
        if (separatorIndex < 0
            || !DateTimeOffset.TryParse(stored[(separatorIndex + 1)..], out var expiresAt)
            || expiresAt < DateTimeOffset.UtcNow
            || !CodesMatch(request.Code, stored[..separatorIndex]))
        {
            return Results.BadRequest("The verification code is invalid or has expired.");
        }

        user.ConfirmEmail();

        // Rotate the security stamp so any tokens issued before confirmation (e.g. password reset
        // tokens) are invalidated, then discard the OTP. Both operations persist the user changes.
        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        var removeResult = await userManager.RemoveAuthenticationTokenAsync(user, EmailConfirmationOtpProvider, EmailConfirmationOtpName);

        if (!stampResult.Succeeded || !removeResult.Succeeded)
        {
            return Results.BadRequest("Failed to confirm the email address.");
        }

        return Results.Ok(true);
    }

    private static bool CodesMatch(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
