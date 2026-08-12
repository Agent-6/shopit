namespace ShopIt.Identity.Application.Contracts.Models;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);
