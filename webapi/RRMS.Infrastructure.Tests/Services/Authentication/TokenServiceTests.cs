using System.Security.Cryptography;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class TokenServiceTests
{
    [Fact]
    public void ValidateAccessToken_ShouldAcceptToken_WhenSignatureAndClaimsAreValid()
    {
        var service = CreateService();
        var id = Guid.NewGuid();
        var token = service.CreateAccessToken(id, "TENANT", "session");

        var principal = service.ValidateAccessToken(token);

        principal.FindFirst("sub")!.Value.Should().Be(id.ToString());
        principal.FindFirst("activeRole")!.Value.Should().Be("TENANT");
        principal.FindFirst("sessionId")!.Value.Should().Be("session");
    }

    [Fact]
    public void ValidateAccessToken_ShouldRejectRefreshToken_WhenTokensHaveDifferentPurposes()
    {
        var service = CreateService();
        var token = service.CreateRefreshToken(Guid.NewGuid(), "TENANT");

        var action = () => service.ValidateAccessToken(token);

        action.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void ValidateCsrfToken_ShouldRejectDifferentSessionOrHeader_WhenTokenIsReplayed()
    {
        var service = CreateService();
        var token = service.CreateCsrfToken("session-a");

        var valid = service.ValidateCsrfToken("session-a", token, token);

        valid.Should().BeTrue();
        service.ValidateCsrfToken("session-b", token, token).Should().BeFalse();
        service.ValidateCsrfToken("session-a", token, token + "x").Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldRejectKeys_WhenKeysAreReused()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var options = new AuthOptions();
        options.Credentials.AccessToken.SecretKey = key;
        options.Credentials.RefreshToken.SecretKey = key;
        var csrf = new CsrfOptions { SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) };

        var action = () => new TokenService(Microsoft.Extensions.Options.Options.Create(options), Microsoft.Extensions.Options.Options.Create(csrf));

        action.Should().Throw<InvalidOperationException>();
    }

    private static TokenService CreateService()
    {
        var auth = new AuthOptions();
        auth.Credentials.AccessToken.SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        auth.Credentials.RefreshToken.SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var csrf = new CsrfOptions { SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) };
        return new TokenService(Microsoft.Extensions.Options.Options.Create(auth), Microsoft.Extensions.Options.Options.Create(csrf));
    }
}
