using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class AuthenticationServiceTests
{
    private readonly Mock<IAuthenticationChallengeStore> _challenges = new();
    private readonly Mock<IAuthenticationDataService> _data = new();
    private readonly Mock<IAuthenticationEmailService> _email = new();
    private readonly Mock<IPasswordService> _passwords = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IHostEnvironment> _environment = new();
    private readonly Mock<Serilog.ILogger> _logger = new();

    [Fact]
    public async Task LoginAsync_ShouldReturnAccessAndCookies_WhenPasswordAndRoleAreValid()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, Email = "test@example.org", IsEmailVerified = true, Status = UserStatus.Active };
        _challenges.Setup(x => x.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _data.Setup(x => x.FindCredentialsAsync("test@example.org", It.IsAny<CancellationToken>())).ReturnsAsync([new UserCredential { UserId = id, User = user, PasswordHash = "hash" }]);
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["TENANT"]);
        _data.Setup(x => x.FindUserAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwords.Setup(x => x.Verify("password", "hash")).Returns(true);
        _tokens.Setup(x => x.CreateRefreshToken(id, "TENANT")).Returns("refresh");
        _tokens.Setup(x => x.ValidateRefreshToken("refresh")).Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("jti", "session")])));
        _tokens.Setup(x => x.CreateCsrfToken("session")).Returns("csrf");
        _tokens.Setup(x => x.CreateAccessToken(id, "TENANT", "session")).Returns("access");
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestModel { Operation = "AUTHENTICATE", Identity = "Test@Example.org", Password = "password", RemoteAddress = "127.0.0.1" });

        result.State.Should().Be("AUTHENTICATED");
        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh");
        result.CsrfToken.Should().Be("csrf");
    }

    [Fact]
    public async Task LoginAsync_ShouldRejectCredentials_WhenPasswordIsIncorrect()
    {
        var id = Guid.NewGuid();
        _challenges.Setup(x => x.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _data.Setup(x => x.FindCredentialsAsync("x@example.org", It.IsAny<CancellationToken>())).ReturnsAsync([new UserCredential
        {
            UserId = id, User = new User { IsEmailVerified = true, Status = UserStatus.Active }, PasswordHash = "hash"
        }]);
        var service = CreateService();

        var action = () => service.LoginAsync(new LoginRequestModel { Operation = "AUTHENTICATE", Identity = "x@example.org", Password = "wrong", RemoteAddress = "127.0.0.1" });

        await action.Should().ThrowAsync<UnauthorizedException>();
        _tokens.Verify(x => x.CreateRefreshToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_ShouldReturnCompletionToken_WhenChallengeIsValid()
    {
        _challenges.Setup(x => x.ExchangeAsync("registration-verify", "verify", "registration-complete", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync("completion");
        var service = CreateService();

        var result = await service.VerifyEmailAsync(new VerifyEmailRequestModel { Token = "verify" });

        result.State.Should().Be("EMAIL_VERIFIED");
        result.CompletionToken.Should().Be("completion");
    }

    [Fact]
    public async Task RegisterAsync_ShouldRejectExpiredToken_WhenCompletingRegistration()
    {
        var service = CreateService();

        var action = () => service.RegisterAsync(new RegistrationRequestModel { Stage = "COMPLETE", Token = "expired", Password = "long-enough-password" });

        await action.Should().ThrowAsync<UnprocessableEntityException>();
        _data.Verify(x => x.CreateVerifiedUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenCompletionChallengeIsValid()
    {
        _challenges.Setup(x => x.ConsumeAsync("registration-complete", "valid", It.IsAny<CancellationToken>())).ReturnsAsync("new@example.org");
        _passwords.Setup(x => x.Hash("long-enough-password")).Returns("hash");
        var service = CreateService();

        var result = await service.RegisterAsync(new RegistrationRequestModel { Stage = "COMPLETE", Token = "valid", Password = "long-enough-password" });

        result.State.Should().Be("REGISTRATION_COMPLETED");
        _data.Verify(x => x.CreateVerifiedUserAsync("new@example.org", "hash", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldRequestRoleSelection_WhenUserHasMultipleRoles()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, IsEmailVerified = true, Status = UserStatus.Active };
        _challenges.Setup(x => x.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _data.Setup(x => x.FindCredentialsAsync("test@example.org", It.IsAny<CancellationToken>())).ReturnsAsync([new UserCredential { UserId = id, User = user, PasswordHash = "hash" }]);
        _passwords.Setup(x => x.Verify("password", "hash")).Returns(true);
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["LANDLORD", "TENANT"]);
        _challenges.Setup(x => x.IssueAsync("role-selection", $"{id}:password", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync("challenge");
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestModel { Operation = "AUTHENTICATE", Identity = "test@example.org", Password = "password", RemoteAddress = "127.0.0.1" });

        result.State.Should().Be("ROLE_SELECTION_REQUIRED");
        result.RoleSelectionToken.Should().Be("challenge");
        result.Roles.Should().BeEquivalentTo(["LANDLORD", "TENANT"]);
        _tokens.Verify(x => x.CreateRefreshToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRejectRevokedRole_WhenRoleIsNoLongerAssigned()
    {
        var id = Guid.NewGuid();
        _tokens.Setup(x => x.ValidateRefreshToken("refresh")).Returns(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", id.ToString()), new Claim("activeRole", "TENANT"), new Claim("jti", "session")])));
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["LANDLORD"]);
        var service = CreateService();

        var action = () => service.RefreshAsync(new RefreshRequestModel { RefreshToken = "refresh" });

        await action.Should().ThrowAsync<UnauthorizedException>();
        _tokens.Verify(x => x.CreateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private AuthenticationService CreateService() => new(_challenges.Object, _data.Object, _email.Object, _passwords.Object,
        _tokens.Object, Microsoft.Extensions.Options.Options.Create(new AuthOptions()), _environment.Object, _logger.Object);
}
