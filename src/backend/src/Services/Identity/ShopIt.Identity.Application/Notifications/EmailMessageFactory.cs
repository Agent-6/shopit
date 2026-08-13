using ShopIt.Notifications.Application.Contracts.Events;

namespace ShopIt.Identity.Application.Notifications;

/// <summary>
/// Builds the <see cref="SendEmailIntegrationEvent"/> messages published by the Identity
/// service for each email flow it owns (invitations, password reset, email confirmation).
/// </summary>
public static class EmailMessageFactory
{
    public static SendEmailIntegrationEvent Invitation(
        EmailNotificationOptions options,
        Guid userId,
        string email,
        string activationToken,
        DateTimeOffset expiresAt)
    {
        var baseUrl = options.AuthBaseUrl.TrimEnd('/');
        var activationLink = $"{baseUrl}/Account/Activate?userId={userId}&token={Uri.EscapeDataString(activationToken)}&clientId=angular-spa";

        return new SendEmailIntegrationEvent(
            userId,
            email,
            "Activate your ShopIt account",
            $"You've been invited to join ShopIt. Click this link to set your password and activate your account: {activationLink}" +
            $"\n\nThe invitation link expires on {expiresAt:g} (UTC).");
    }

    public static SendEmailIntegrationEvent PasswordReset(
        EmailNotificationOptions options,
        Guid userId,
        string email,
        string token)
    {
        var baseUrl = options.AuthBaseUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        return new SendEmailIntegrationEvent(
            userId,
            email,
            "Reset your ShopIt password",
            $"We received a request to reset your password. Click this link to choose a new one: {resetLink}" +
            "\n\nIf you didn't request a password reset, you can safely ignore this email.");
    }

    public static SendEmailIntegrationEvent EmailConfirmationOtp(
        Guid userId,
        string email,
        string code)
    {
        return new SendEmailIntegrationEvent(
            userId,
            email,
            "Your ShopIt verification code",
            $"Your email confirmation code is {code}. It expires in 10 minutes.");
    }

    public static SendEmailIntegrationEvent AccountActivated(
        Guid userId,
        string email)
    {
        return new SendEmailIntegrationEvent(
            userId,
            email,
            "Welcome to ShopIt",
            "Your ShopIt account is now active. You can sign in and start using the portal.");
    }
}
