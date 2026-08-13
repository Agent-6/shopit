namespace ShopIt.Identity.Application.Users.Activation;

public record ActivationTokenValidationResult(bool IsValid, bool IsExpired, string? Error)
{
    public static ActivationTokenValidationResult Valid() => new(true, false, null);

    public static ActivationTokenValidationResult Expired() => new(false, true, "The invitation link has expired.");

    public static ActivationTokenValidationResult Invalid() => new(false, false, "The invitation link is invalid.");
}
