using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace ShopIt.Identity.Application.Users.Activation;

/// <summary>
/// Issues and validates activation tokens with ASP.NET Core data protection.
/// Tokens are encrypted and authenticated with the purpose string
/// <c>"UserAccountActivation"</c> and embed the user id plus a UTC expiry, so no
/// server-side token storage is required — the token is self-contained and unforgeable.
/// </summary>
public class ActivationTokenProvider : IActivationTokenProvider
{
    public const string Purpose = "UserAccountActivation";

    private static readonly TimeSpan DefaultValidity = TimeSpan.FromHours(48);

    private readonly IDataProtector _protector;

    public ActivationTokenProvider(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public ActivationToken Issue(Guid userId)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(DefaultValidity);
        var payload = $"{userId:N}|{expiresAt:O}";
        var token = Base64UrlEncode(_protector.Protect(Encoding.UTF8.GetBytes(payload)));
        return new ActivationToken(token, expiresAt);
    }

    public ActivationTokenValidationResult Validate(Guid userId, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ActivationTokenValidationResult.Invalid();
        }

        try
        {
            var payload = Encoding.UTF8.GetString(_protector.Unprotect(Base64UrlDecode(token)));

            var separatorIndex = payload.IndexOf('|');
            if (separatorIndex < 0
                || !Guid.TryParseExact(payload[..separatorIndex], "N", out var tokenUserId)
                || tokenUserId != userId)
            {
                return ActivationTokenValidationResult.Invalid();
            }

            if (!DateTimeOffset.TryParse(payload[(separatorIndex + 1)..], out var expiresAt)
                || expiresAt < DateTimeOffset.UtcNow)
            {
                return ActivationTokenValidationResult.Expired();
            }

            return ActivationTokenValidationResult.Valid();
        }
        catch (CryptographicException)
        {
            // Tampered or produced with a different key ring.
            return ActivationTokenValidationResult.Invalid();
        }
        catch (FormatException)
        {
            // Malformed base64url payload.
            return ActivationTokenValidationResult.Invalid();
        }
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string token)
    {
        var base64 = token.Replace('-', '+').Replace('_', '/');
        base64 = (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64
        };

        return Convert.FromBase64String(base64);
    }
}
