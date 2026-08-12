namespace ShopIt.Identity.Application.Contracts.Models;

/// <summary>
/// Result of requesting an email confirmation code. The code is only populated in
/// development/mock mode; in production it would be delivered by email.
/// </summary>
public record SendEmailConfirmationOtpResponse(string Email, string? Code);
