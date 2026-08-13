namespace ShopIt.Identity.Application.Users.Activation;

public record ActivationToken(string Token, DateTimeOffset ExpiresAt);
