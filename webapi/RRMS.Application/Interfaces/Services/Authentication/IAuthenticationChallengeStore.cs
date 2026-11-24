namespace RRMS.Application.Interfaces.Services.Authentication;

public interface IAuthenticationChallengeStore
{
    Task<string> IssueAsync(string purpose, string data, TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task<string?> FindAsync(string purpose, string? token, CancellationToken cancellationToken = default);
    Task<string?> ConsumeAsync(string purpose, string? token, CancellationToken cancellationToken = default);
    Task<string?> ExchangeAsync(string fromPurpose, string? token, string toPurpose, TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task<bool> AllowAsync(string scope, string identifier, long limit, TimeSpan window, CancellationToken cancellationToken = default);
}
