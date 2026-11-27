using RRMS.Domain.Constants.Authentication;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using StackExchange.Redis;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Stores authentication tickets and user-session indexes in Redis.
/// It supports session renewal, revocation, and atomic rotation while respecting expiration.
/// </summary>
public sealed class RedisTicketStore : ITicketStore
{
    private readonly IDatabase _database;
    private const string Prefix = "auth:session:";
    private const string AddScript = "if redis.call('EXISTS', KEYS[1]) ~= 0 then return 0 end redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2]); redis.call('SADD', KEYS[2], KEYS[1]); return 1";
    private const string RenewScript = "if redis.call('EXISTS', KEYS[1]) == 0 then return 0 end redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2], 'XX'); return 1";
    private const string RemoveScript = "redis.call('DEL', KEYS[1]); redis.call('SREM', KEYS[2], KEYS[1]); return 1";
    private const string RotateScript = "if redis.call('EXISTS', KEYS[1]) == 0 or redis.call('EXISTS', KEYS[2]) ~= 0 then return 0 end redis.call('SET', KEYS[2], ARGV[1], 'PX', ARGV[2]); redis.call('SREM', KEYS[3], KEYS[1]); redis.call('SADD', KEYS[3], KEYS[2]); redis.call('DEL', KEYS[1]); return 1";
    private const string RemoveAllScript = "local keys = redis.call('SMEMBERS', KEYS[1]); for _, key in ipairs(keys) do redis.call('DEL', key) end; redis.call('DEL', KEYS[1]); return #keys";

    public RedisTicketStore(IConnectionMultiplexer redis) => _database = redis.GetDatabase();

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var user = UserId(ticket);
        var ttl = Lifetime(ticket);
        var key = Prefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var result = await _database.ScriptEvaluateAsync(AddScript, [key, UserKey(user)], [TicketSerializer.Default.Serialize(ticket), (long)ttl.TotalMilliseconds]);
        if ((long)result != 1)
        {
            throw new InvalidOperationException("Could not create session.");
        }

        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (!ValidKey(key))
        {
            return;
        }

        var ttl = Lifetime(ticket);
        await _database.ScriptEvaluateAsync(RenewScript, [key], [TicketSerializer.Default.Serialize(ticket), (long)ttl.TotalMilliseconds]);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!ValidKey(key))
        {
            return null;
        }

        var value = await _database.StringGetAsync(key);
        if (value.IsNull)
        {
            return null;
        }

        var ticket = TicketSerializer.Default.Deserialize((byte[])value!);
        if (ticket == null || !IsValid(ticket))
        {
            return null;
        }

        return ticket;
    }

    public async Task RemoveAsync(string key)
    {
        var ticket = await RetrieveAsync(key);
        if (ticket != null)
        {
            await _database.ScriptEvaluateAsync(RemoveScript, [key, UserKey(UserId(ticket))], []);
        }
        else
        {
            if (ValidKey(key))
            {
                await _database.KeyDeleteAsync(key);
            }
        }
    }

    public async Task<string?> RotateAsync(string oldKey, AuthenticationTicket ticket)
    {
        if (!ValidKey(oldKey))
        {
            return null;
        }

        var userId = UserId(ticket);
        var key = Prefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var result = await _database.ScriptEvaluateAsync(RotateScript,
            [oldKey, key, UserKey(userId)], [TicketSerializer.Default.Serialize(ticket), (long)Lifetime(ticket).TotalMilliseconds]);
        return (long)result == 1 ? key : null;
    }

    public async Task RemoveAllAsync(Guid userId) => await _database.ScriptEvaluateAsync(RemoveAllScript, [UserKey(userId)], []);

    public async Task<IReadOnlyList<AuthenticationTicket>> FindUserSessionsAsync(Guid userId)
    {
        var index = UserKey(userId);
        var members = await _database.SetMembersAsync(index);
        var tickets = new List<AuthenticationTicket>();
        foreach (var member in members)
        {
            var key = member.ToString();
            var ticket = await RetrieveAsync(key);
            if (ticket == null)
            {
                await _database.SetRemoveAsync(index, member);
            }
            else
            {
                tickets.Add(ticket);
            }
        }
        return tickets;
    }

    public async Task<bool> RemoveReferenceAsync(Guid userId, string reference)
    {
        var index = UserKey(userId);
        var members = await _database.SetMembersAsync(index);
        foreach (var member in members)
        {
            var ticket = await RetrieveAsync(member.ToString());
            if (ticket == null)
            {
                await _database.SetRemoveAsync(index, member);
                continue;
            }
            if (ticket.Principal.FindFirst(AuthenticationClaimTypes.SessionReference)?.Value != reference)
            {
                continue;
            }

            await _database.ScriptEvaluateAsync(RemoveScript, [member.ToString(), index], []);
            return true;
        }
        return false;
    }

    private static Guid UserId(AuthenticationTicket ticket) => Guid.TryParse(ticket.Principal.FindFirst(AuthenticationClaimTypes.Subject)?.Value, out var id) ? id : throw new InvalidOperationException("Missing session user.");
    private static string UserKey(Guid id) => $"auth:user:{id}:sessions";
    private static bool ValidKey(string? key) => key != null && key.StartsWith(Prefix, StringComparison.Ordinal) && key.Length == Prefix.Length + 43 && key.AsSpan(Prefix.Length).ToArray().All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    private static bool IsValid(AuthenticationTicket ticket) => ticket.Properties.ExpiresUtc > DateTimeOffset.UtcNow && ticket.Properties.Items.TryGetValue("absoluteExpires", out var expires) && DateTimeOffset.TryParse(expires, out var absolute) && absolute > DateTimeOffset.UtcNow;
    private static TimeSpan Lifetime(AuthenticationTicket ticket)
    {
        if (!IsValid(ticket))
        {
            throw new InvalidOperationException("Session has expired.");
        }

        var absolute = DateTimeOffset.Parse(ticket.Properties.Items["absoluteExpires"]!);
        var end = ticket.Properties.ExpiresUtc!.Value < absolute ? ticket.Properties.ExpiresUtc.Value : absolute;
        return end - DateTimeOffset.UtcNow;
    }
}
