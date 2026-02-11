using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SecureFileStatementDelivery.Api.IntegrationTests;

internal static class JwtTokens
{
    public static string Create(
        string signingKey,
        string issuer,
        string audience,
        string subject,
        string? customerId,
        IEnumerable<string> roles,
        DateTimeOffset nowUtc,
        TimeSpan lifetime)
    {
        if (signingKey.Length < 32)
        {
            throw new ArgumentException("Signing key must be 32+ characters", nameof(signingKey));
        }

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Iat, nowUtc.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            claims.Add(new Claim("customerId", customerId));
        }

        foreach (var role in roles ?? Array.Empty<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: nowUtc.UtcDateTime,
            expires: nowUtc.Add(lifetime).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
