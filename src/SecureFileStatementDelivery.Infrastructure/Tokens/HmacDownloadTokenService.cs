using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Application.Downloads;
using SecureFileStatementDelivery.Infrastructure.Time;

namespace SecureFileStatementDelivery.Infrastructure.Tokens;

public sealed class HmacDownloadTokenService : IDownloadTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly byte[] _secret;
    private readonly ITimeProvider _time;

    private static readonly TimeSpan MaxTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AllowedIssuedAtSkew = TimeSpan.FromMinutes(2);
    private const int MaxTokenChars = 4096;

    public HmacDownloadTokenService(IOptions<DownloadTokenOptions> options, ITimeProvider time)
    {
        var raw = options.Value.Secret;
        _secret = TryDecodeBase64(raw) ?? Encoding.UTF8.GetBytes(raw);
        if (_secret.Length < 32)
        {
            throw new InvalidOperationException("Download token secret is too short; use 32+ bytes.");
        }

        _time = time;
    }

    public string CreateToken(CreateDownloadTokenRequest request)
    {
        var issuedAtUtc = _time.UtcNow;
        var payload = new TokenPayload(
            TokenId: Guid.NewGuid(),
            StatementId: request.StatementId,
            CustomerId: request.CustomerId,
            IssuedAtUtc: issuedAtUtc,
            ExpiresAtUtc: request.ExpiresAtUtc);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadB64 = Base64UrlEncode(payloadBytes);

        var sig = ComputeHmacBase64Url(payloadB64);
        return $"{payloadB64}.{sig}";
    }

    public bool TryValidate(string token, out ValidatedDownloadToken validated)
    {
        validated = new ValidatedDownloadToken(
            TokenId: Guid.Empty,
            StatementId: Guid.Empty,
            CustomerId: string.Empty,
            IssuedAtUtc: DateTimeOffset.MinValue,
            ExpiresAtUtc: DateTimeOffset.MinValue);

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Length > MaxTokenChars)
        {
            return false;
        }

        token = token.Trim();

        var dotIndex = token.IndexOf('.');
        if (dotIndex <= 0 || dotIndex == token.Length - 1)
        {
            return false;
        }

        var payloadB64 = token[..dotIndex];
        var sigB64 = token[(dotIndex + 1)..];

        if (payloadB64.Length == 0 || sigB64.Length == 0)
        {
            return false;
        }

        byte[] providedSig;
        try
        {
            providedSig = Base64UrlDecode(sigB64);
        }
        catch
        {
            return false;
        }

        var expectedSig = ComputeHmacBytes(payloadB64);
        if (!CryptographicOperations.FixedTimeEquals(expectedSig, providedSig))
        {
            return false;
        }

        byte[] payloadBytes;
        try
        {
            payloadBytes = Base64UrlDecode(payloadB64);
        }
        catch
        {
            return false;
        }

        TokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TokenPayload>(payloadBytes, JsonOptions);
        }
        catch
        {
            return false;
        }

        if (payload is null
            || payload.TokenId == Guid.Empty
            || payload.StatementId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.CustomerId))
        {
            return false;
        }

        if (payload.ExpiresAtUtc <= payload.IssuedAtUtc)
        {
            return false;
        }

        var lifetime = payload.ExpiresAtUtc - payload.IssuedAtUtc;
        if (lifetime <= TimeSpan.Zero || lifetime > MaxTokenLifetime)
        {
            return false;
        }

        var now = _time.UtcNow;
        if (payload.IssuedAtUtc > now.Add(AllowedIssuedAtSkew))
        {
            return false;
        }

        validated = new ValidatedDownloadToken(
            payload.TokenId,
            payload.StatementId,
            payload.CustomerId,
            payload.IssuedAtUtc,
            payload.ExpiresAtUtc);
        return true;
    }

    private byte[] ComputeHmacBytes(string payloadB64)
    {
        using var hmac = new HMACSHA256(_secret);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
    }

    private string ComputeHmacBase64Url(string payloadB64)
    {
        return Base64UrlEncode(ComputeHmacBytes(payloadB64));
    }

    private static byte[]? TryDecodeBase64(string raw)
    {
        try
        {
            return Convert.FromBase64String(raw);
        }
        catch
        {
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');

        var mod = s.Length % 4;
        if (mod == 2)
        {
            s += "==";
        }
        else if (mod == 3)
        {
            s += "=";
        }
        else if (mod == 1)
        {
            throw new FormatException("Invalid base64url string.");
        }

        return Convert.FromBase64String(s);
    }

    private sealed record TokenPayload(
        Guid TokenId,
        Guid StatementId,
        string CustomerId,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc);
}
