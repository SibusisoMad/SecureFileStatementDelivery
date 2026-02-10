using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Application.Downloads;

namespace SecureFileStatementDelivery.Infrastructure.Tokens;

public sealed class HmacDownloadTokenService : IDownloadTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly byte[] _secret;

    public HmacDownloadTokenService(IOptions<DownloadTokenOptions> options)
    {
        var raw = options.Value.Secret;
        _secret = TryDecodeBase64(raw) ?? Encoding.UTF8.GetBytes(raw);
        if (_secret.Length < 32)
        {
            throw new InvalidOperationException("Download token secret is too short; use 32+ bytes.");
        }
    }

    public string CreateToken(CreateDownloadTokenRequest request)
    {
        var payload = new TokenPayload(request.StatementId, request.CustomerId, request.ExpiresAtUtc);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadB64 = Base64UrlEncode(payloadBytes);

        var sig = ComputeHmac(payloadB64);
        return $"{payloadB64}.{sig}";
    }

    public bool TryValidate(string token, out ValidatedDownloadToken validated)
    {
        validated = new ValidatedDownloadToken(Guid.Empty, string.Empty, DateTimeOffset.MinValue);

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var payloadB64 = parts[0];
        var sigB64 = parts[1];

        var expected = ComputeHmac(payloadB64);
        if (!FixedTimeEquals(expected, sigB64))
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

        if (payload is null || payload.StatementId == Guid.Empty || string.IsNullOrWhiteSpace(payload.CustomerId))
        {
            return false;
        }

        validated = new ValidatedDownloadToken(payload.StatementId, payload.CustomerId, payload.ExpiresAtUtc);
        return true;
    }

    private string ComputeHmac(string payloadB64)
    {
        using var hmac = new HMACSHA256(_secret);
        var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
        return Base64UrlEncode(sig);
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

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(ba, bb);
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

        return Convert.FromBase64String(s);
    }

    private sealed record TokenPayload(Guid StatementId, string CustomerId, DateTimeOffset ExpiresAtUtc);
}
