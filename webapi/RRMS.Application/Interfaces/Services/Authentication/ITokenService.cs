using System.Security.Claims;

namespace RRMS.Application.Interfaces.Services.Authentication;

public interface ITokenService
{
    string CreateAccessToken(Guid userId, string activeRole, string sessionId);
    string CreateRefreshToken(Guid userId, string activeRole);
    ClaimsPrincipal ValidateAccessToken(string token);
    ClaimsPrincipal ValidateRefreshToken(string token);
    string CreateCsrfToken(string sessionId);
    bool ValidateCsrfToken(string? sessionId, string? cookieToken, string? headerToken);
}
