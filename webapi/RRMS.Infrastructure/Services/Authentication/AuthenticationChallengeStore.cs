using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using RRMS.Application.Interfaces.Services.Authentication;
using StackExchange.Redis;

namespace RRMS.Infrastructure.Services.Authentication;

public sealed class AuthenticationChallengeStore : IAuthenticationChallengeStore
{
    private const string ExchangeScript = "local value = redis.call('GET', KEYS[1]); if not value then return nil end; if not redis.call('SET', KEYS[2], value, 'PX', ARGV[1], 'NX') then return nil end; redis.call('DEL', KEYS[1]); return value";
    private const string RateLimitScript = "local count = redis.call('INCR', KEYS[1]); if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end; return count";
    private readonly IDatabase _database;

    public AuthenticationChallengeStore(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<string> IssueAsync(string purpose, string data, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ValidateLifetime(lifetime);
        var token = NewToken();
        var stored = await _database.StringSetAsync(Key(purpose, token), data, lifetime, When.NotExists).WaitAsync(cancellationToken);
        return stored ? token : throw new InvalidOperationException("Unable to store authentication challenge.");
    }

    public async Task<string?> FindAsync(string purpose, string? token, CancellationToken cancellationToken = default)
    {
        if (!ValidToken(token)) return null;
        var value = await _database.StringGetAsync(Key(purpose, token!)).WaitAsync(cancellationToken);
        return value.IsNull ? null : value.ToString();
    }

    public async Task<string?> ConsumeAsync(string purpose, string? token, CancellationToken cancellationToken = default)
    {
        if (!ValidToken(token)) return null;
        var value = await _database.StringGetDeleteAsync(Key(purpose, token!)).WaitAsync(cancellationToken);
        return value.IsNull ? null : value.ToString();
    }

    public async Task<string?> ExchangeAsync(string fromPurpose, string? token, string toPurpose, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        if (!ValidToken(token)) return null;
        ValidateLifetime(lifetime);
        var next = NewToken();
        var result = await _database.ScriptEvaluateAsync(ExchangeScript,
            [Key(fromPurpose, token!), Key(toPurpose, next)], [(long)lifetime.TotalMilliseconds]).WaitAsync(cancellationToken);
        return result.IsNull ? null : next;
    }

    public async Task<bool> AllowAsync(string scope, string identifier, long limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        if (limit < 1 || string.IsNullOrWhiteSpace(identifier)) throw new ArgumentException("Invalid rate limit.");
        ValidateLifetime(window);
        var result = await _database.ScriptEvaluateAsync(RateLimitScript,
            [$"rrms:auth:limit:{Purpose(scope)}:{Digest(identifier)}"], [(long)window.TotalMilliseconds]).WaitAsync(cancellationToken);
        if (result.IsNull) throw new InvalidOperationException("Unable to apply authentication rate limit.");
        return (long)result <= limit;
    }

    private static string NewToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    private static bool ValidToken(string? token) => token != null && Regex.IsMatch(token, "^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant);
    private static string Key(string purpose, string token) => $"rrms:auth:challenge:{Purpose(purpose)}:{Digest(token)}";
    private static string Digest(string value) => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Purpose(string purpose) => purpose != null && Regex.IsMatch(purpose, "^[a-z-]{1,40}$", RegexOptions.CultureInvariant) ? purpose : throw new ArgumentException("Invalid challenge purpose.");
    private static void ValidateLifetime(TimeSpan lifetime)
    {
        if (lifetime.TotalMilliseconds < 1 || lifetime.TotalMilliseconds > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(lifetime));
    }
}
