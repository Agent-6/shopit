namespace ShopIt.Identity.Application.Contracts.Models;

/// <summary>
/// Result of requesting a password reset. The token is only populated in development/mock
/// mode so the caller can render the reset link; in production it would be delivered by email.
/// </summary>
public record ForgotPasswordResponse(string Email, string? Token);
