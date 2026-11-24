using System.Buffers.Binary;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Authentication;

public sealed class TokenService : ITokenService
{
    private readonly TokenOptions _access;
    private readonly TokenOptions _refresh;
    private readonly byte[] _accessKey;
    private readonly byte[] _refreshKey;
    private readonly byte[] _csrfKey;

    public TokenService(IOptions<AuthOptions> authOptions, IOptions<CsrfOptions> csrfOptions)
    {
        _access = authOptions.Value.Credentials.AccessToken;
        _refresh = authOptions.Value.Credentials.RefreshToken;
        _accessKey = DecodeKey(_access.SecretKey);
        _refreshKey = DecodeKey(_refresh.SecretKey);
        _csrfKey = DecodeKey(csrfOptions.Value.SecretKey);
        if (CryptographicOperations.FixedTimeEquals(_accessKey, _refreshKey) ||
            CryptographicOperations.FixedTimeEquals(_accessKey, _csrfKey) ||
            CryptographicOperations.FixedTimeEquals(_refreshKey, _csrfKey))
        {
            throw new InvalidOperationException("Authentication signing keys must be independent.");
        }
        ValidateOptions(_access);
        ValidateOptions(_refresh);
    }

    public string CreateAccessToken(Guid userId, string activeRole, string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return CreateToken(userId, activeRole, sessionId, _access, _accessKey, "access");
    }

    public string CreateRefreshToken(Guid userId, string activeRole) =>
        CreateToken(userId, activeRole, null, _refresh, _refreshKey, "refresh");

    public ClaimsPrincipal ValidateAccessToken(string token) => Validate(token, _access, _accessKey, "access");
    public ClaimsPrincipal ValidateRefreshToken(string token) => Validate(token, _refresh, _refreshKey, "refresh");

    public string CreateCsrfToken(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var nonce = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncoder.Encode(SignCsrf(sessionId, nonce)) + "." + Base64UrlEncoder.Encode(nonce);
    }

    public bool ValidateCsrfToken(string? sessionId, string? cookieToken, string? headerToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken) ||
            cookieToken.Length > 128 || headerToken.Length > 128 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookieToken), Encoding.UTF8.GetBytes(headerToken)))
        {
            return false;
        }
        var parts = cookieToken.Split('.');
        if (parts.Length != 2) return false;
        try
        {
            var signature = Base64UrlEncoder.DecodeBytes(parts[0]);
            var nonce = Base64UrlEncoder.DecodeBytes(parts[1]);
            return signature.Length == 32 && nonce.Length == 32 &&
                CryptographicOperations.FixedTimeEquals(signature, SignCsrf(sessionId, nonce));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string CreateToken(Guid userId, string role, string? sessionId, TokenOptions options, byte[] key, string type)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("tokenType", type),
            new("activeRole", role)
        };
        if (sessionId != null) claims.Add(new Claim("sessionId", sessionId));
        var jwt = new JwtSecurityToken(options.Issuer, options.Audience, claims, now, now.Add(options.Expiration),
            new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static ClaimsPrincipal Validate(string token, TokenOptions options, byte[] key, string type)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = options.Issuer,
            ValidateAudience = true, ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero
        }, out _);
        if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out _) ||
            string.IsNullOrWhiteSpace(principal.FindFirst("jti")?.Value) ||
            string.IsNullOrWhiteSpace(principal.FindFirst("iat")?.Value) ||
            string.IsNullOrWhiteSpace(principal.FindFirst("activeRole")?.Value) ||
            principal.FindFirst("tokenType")?.Value != type ||
            (type == "access" && string.IsNullOrWhiteSpace(principal.FindFirst("sessionId")?.Value)))
        {
            throw new SecurityTokenValidationException("Invalid token claims.");
        }
        return principal;
    }

    private byte[] SignCsrf(string sessionId, byte[] nonce)
    {
        var id = Encoding.UTF8.GetBytes(sessionId);
        var input = new byte[4 + id.Length + nonce.Length];
        BinaryPrimitives.WriteInt32BigEndian(input.AsSpan(0, 4), id.Length);
        id.CopyTo(input, 4);
        nonce.CopyTo(input, 4 + id.Length);
        return HMACSHA256.HashData(_csrfKey, input);
    }

    private static byte[] DecodeKey(string value)
    {
        try
        {
            var key = Convert.FromBase64String(value);
            if (key.Length >= 32) return key;
        }
        catch (FormatException) { }
        throw new InvalidOperationException("Authentication signing keys must be Base64 and at least 32 bytes.");
    }

    private static void ValidateOptions(TokenOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience) || options.Expiration <= TimeSpan.Zero)
            throw new InvalidOperationException("Token issuer, audience, and positive expiration are required.");
    }
}
