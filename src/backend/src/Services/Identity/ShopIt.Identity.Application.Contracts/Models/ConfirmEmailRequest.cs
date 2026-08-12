namespace ShopIt.Identity.Application.Contracts.Models;

public record ConfirmEmailRequest(string Email, string Code);
