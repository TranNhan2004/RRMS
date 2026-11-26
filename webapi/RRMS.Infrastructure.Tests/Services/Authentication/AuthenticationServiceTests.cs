using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using RRMS.Application.Interfaces.Contexts;
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
    private readonly Mock<IHostEnvironment> _environment = new();
    private readonly Mock<Serilog.ILogger> _logger = new();

    [Fact]
    public async Task LoginAsync_ShouldReturnIdentityAndRole_WhenPasswordAndRoleAreValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "test@example.org",
            IsEmailVerified = true,
            Status = UserStatus.Active
        };
        _challenges.Setup(x => x.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _data.Setup(x => x.FindCredentialsAsync("test@example.org", It.IsAny<CancellationToken>())).ReturnsAsync([new UserCredential
        {
            UserId = id,
            User = user,
            PasswordHash = "hash"
        }]);
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        _data.Setup(x => x.FindUserAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwords.Setup(x => x.Verify("password", "hash")).Returns(true);
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequestModel
        {
            Identity = "Test@Example.org",
            Password = "password"
        });

        // Assert
        result.State.Should().Be("AUTHENTICATED");
        result.UserId.Should().Be(id);
        result.ActiveRole.Should().Be("Tenant");
        result.AuthenticationMethod.Should().Be("password");
    }

    [Fact]
    public async Task LoginAsync_ShouldRejectCredentials_WhenPasswordIsIncorrect()
    {
        // Arrange
        var id = Guid.NewGuid();
        _challenges.Setup(x => x.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _data.Setup(x => x.FindCredentialsAsync("x@example.org", It.IsAny<CancellationToken>())).ReturnsAsync([new UserCredential
        {
            UserId = id,
            User = new User
            {
                IsEmailVerified = true,
                Status = UserStatus.Active
            },
            PasswordHash = "hash"
        }]);
        var service = CreateService();

        // Act
        var action = () => service.LoginAsync(new LoginRequestModel
        {
            Identity = "x@example.org",
            Password = "wrong"
        });

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task VerifyEmailAsync_ShouldReturnCompletionToken_WhenChallengeIsValid()
    {
        // Arrange
        _challenges.Setup(x => x.ExchangeAsync("registration-verify", "verify", "registration-complete", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync("completion");
        var service = CreateRegistrationService();

        // Act
        var result = await service.VerifyEmailAsync(new VerifyEmailRequestModel
        {
            Token = "verify"
        });

        // Assert
        result.State.Should().Be("EMAIL_VERIFIED");
        result.CompletionToken.Should().Be("completion");
    }

    [Fact]
    public async Task RegisterAsync_ShouldRejectExpiredToken_WhenCompletingRegistration()
    {
        // Arrange
        var service = CreateRegistrationService();

        // Act
        var action = () => service.RegisterAsync(new RegistrationRequestModel
        {
            Stage = "COMPLETE",
            Token = "expired",
            Password = "long-enough-password"
        });

        // Assert
        await action.Should().ThrowAsync<UnprocessableEntityException>();
        _data.Verify(x => x.CreateVerifiedUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenCompletionChallengeIsValid()
    {
        // Arrange
        _challenges.Setup(x => x.ConsumeAsync("registration-complete", "valid", It.IsAny<CancellationToken>())).ReturnsAsync("new@example.org");
        _passwords.Setup(x => x.Hash("long-enough-password")).Returns("hash");
        var service = CreateRegistrationService();

        // Act
        var result = await service.RegisterAsync(new RegistrationRequestModel
        {
            Stage = "COMPLETE",
            Token = "valid",
            Password = "long-enough-password"
        });

        // Assert
        result.State.Should().Be("REGISTRATION_COMPLETED");
        _data.Verify(x => x.CreateVerifiedUserAsync("new@example.org", "hash", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldRequestRoleSelection_WhenUserHasMultipleRoles()
    {
        // Arrange
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            IsEmailVerified = true,
            Status = UserStatus.Active
        };
        _challenges.Setup(x => x.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _data.Setup(x => x.FindCredentialsAsync("test@example.org", It.IsAny<CancellationToken>())).ReturnsAsync([new UserCredential
        {
            UserId = id,
            User = user,
            PasswordHash = "hash"
        }]);
        _passwords.Setup(x => x.Verify("password", "hash")).Returns(true);
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["Landlord", "Tenant"]);
        _challenges.Setup(x => x.IssueAsync("role-selection", $"{id}:password", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync("challenge");
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequestModel
        {
            Identity = "test@example.org",
            Password = "password"
        });

        // Assert
        result.State.Should().Be("ROLE_SELECTION_REQUIRED");
        result.Roles.Should().BeEquivalentTo(["Landlord", "Tenant"]);
    }

    [Fact]
    public async Task LoginAsync_ShouldRejectRoleSelection_WhenChallengeWasAlreadyConsumed()
    {
        // Arrange
        var service = CreateService("expired");

        // Act
        var action = () => service.SelectRoleAsync(new SelectRoleRequestModel
        {
            RoleCode = "Tenant"
        });

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldRejectRoleSelection_WhenRoleIsNotAssigned()
    {
        // Arrange
        var id = Guid.NewGuid();
        _challenges.Setup(x => x.FindAsync("role-selection", "challenge", It.IsAny<CancellationToken>())).ReturnsAsync($"{id}:password");
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        var service = CreateService("challenge");

        // Act
        var action = () => service.SelectRoleAsync(new SelectRoleRequestModel
        {
            RoleCode = "Admin"
        });

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
        _challenges.Verify(x => x.ConsumeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectRoleAsync_ShouldRejectInactiveUser_WhenPendingChallengeIsValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        _challenges.Setup(x => x.FindAsync("role-selection", "challenge", It.IsAny<CancellationToken>())).ReturnsAsync($"{id}:password");
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        _data.Setup(x => x.FindUserAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = id,
            IsEmailVerified = false,
            Status = UserStatus.Active
        });
        var service = CreateService("challenge");

        // Act
        var action = () => service.SelectRoleAsync(new SelectRoleRequestModel
        {
            RoleCode = "Tenant"
        });

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
        _challenges.Verify(x => x.ConsumeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectRoleAsync_ShouldReturnAuthenticatedUser_WhenChallengeAndRoleAreValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var challenge = $"{id}:password";
        _challenges.Setup(x => x.FindAsync("role-selection", "challenge", It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        _challenges.Setup(x => x.ConsumeAsync("role-selection", "challenge", It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        _data.Setup(x => x.FindUserAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = id,
            IsEmailVerified = true,
            Status = UserStatus.Active
        });

        // Act
        var result = await CreateService("challenge").SelectRoleAsync(new SelectRoleRequestModel
        {
            RoleCode = "Tenant"
        });

        // Assert
        result.UserId.Should().Be(id);
        result.ActiveRole.Should().Be("Tenant");
        result.State.Should().Be("AUTHENTICATED");
        _challenges.Verify(x => x.ConsumeAsync("role-selection", "challenge", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnPendingUserAndExpiry_WhenChallengeIsValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        _challenges.Setup(x => x.FindAsync("role-selection", "challenge", It.IsAny<CancellationToken>())).ReturnsAsync($"{id}:password");
        _challenges.Setup(x => x.FindRemainingLifetimeAsync("role-selection", "challenge", It.IsAny<CancellationToken>())).ReturnsAsync(TimeSpan.FromMinutes(4));
        _data.Setup(x => x.FindEligibleRolesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant", "Landlord"]);
        _data.Setup(x => x.FindUserAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = id,
            UserName = "user",
            IsEmailVerified = true,
            Status = UserStatus.Active
        });

        // Act
        var result = await CreateService("challenge").GetPendingSessionAsync();

        // Assert
        result.UserId.Should().Be(id);
        result.UserName.Should().Be("user");
        result.PendingExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnRateLimitError_WhenRequestLimitIsExceeded()
    {
        // Arrange
        _challenges.Setup(x => x.AllowAsync("login-ip", "127.0.0.1", It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var action = () => CreateService().LoginAsync(new LoginRequestModel
        {
            Identity = "user@example.org",
            Password = "password"
        });

        // Assert
        await action.Should().ThrowAsync<RateLimitException>();
    }

    [Fact]
    public async Task RegisterAsync_ShouldSendVerificationLink_WhenDevelopmentUsesHttpUrl()
    {
        // Arrange
        _environment.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Development);
        _challenges.Setup(store => store.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _challenges.Setup(store => store.IssueAsync("registration-verify", "user@example.org", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("verification-token");
        _email.Setup(email => email.SendVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var options = Microsoft.Extensions.Options.Options.Create(new AuthOptions
        {
            Credentials = new CredentialAuthOptions
            {
                VerificationUrl = "http://localhost:4200/verify",
                RegistrationVerificationTtl = TimeSpan.FromMinutes(15)
            }
        });
        var service = new RegistrationService(_challenges.Object, _data.Object, _email.Object, _passwords.Object,
            options, _environment.Object, _logger.Object, new Mock<ICurrentRequestContext>().Object);
        var request = new RegistrationRequestModel
        {
            Stage = "INITIATE",
            Email = "user@example.org"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.State.Should().Be("REGISTRATION_PENDING");
        _email.Verify(email => email.SendVerificationAsync("user@example.org", "http://localhost:4200/verify?token=verification-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Local")]
    public async Task RegisterAsync_ShouldRejectHttpVerificationUrl_WhenEnvironmentIsNotDevelopment(string environmentName)
    {
        // Arrange
        _environment.SetupGet(environment => environment.EnvironmentName).Returns(environmentName);
        var options = Microsoft.Extensions.Options.Options.Create(new AuthOptions
        {
            Credentials = new CredentialAuthOptions
            {
                VerificationUrl = "http://localhost:4200/verify"
            }
        });
        var service = new RegistrationService(_challenges.Object, _data.Object, _email.Object, _passwords.Object,
            options, _environment.Object, _logger.Object, new Mock<ICurrentRequestContext>().Object);
        var request = new RegistrationRequestModel
        {
            Stage = "INITIATE",
            Email = "user@example.org"
        };

        // Act
        var action = () => service.RegisterAsync(request);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        _email.Verify(email => email.SendVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldInvalidateChallenge_WhenEmailCannotBeQueued()
    {
        // Arrange
        _challenges.Setup(x => x.AllowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _data.Setup(x => x.FindEmailExistsAsync("new@example.org", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _challenges.Setup(x => x.IssueAsync("registration-verify", "new@example.org", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync("challenge");
        _email.Setup(x => x.SendVerificationAsync("new@example.org", It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        var options = new AuthOptions
        {
            Credentials = new CredentialAuthOptions
            {
                VerificationUrl = "https://example.org/verify",
                RegistrationVerificationTtl = TimeSpan.FromMinutes(15)
            }
        };
        var service = new RegistrationService(_challenges.Object, _data.Object, _email.Object, _passwords.Object,
            Microsoft.Extensions.Options.Options.Create(options), _environment.Object, _logger.Object, new Mock<ICurrentRequestContext>().Object);

        // Act
        var result = await service.RegisterAsync(new RegistrationRequestModel
        {
            Stage = "INITIATE",
            Email = "new@example.org",
        });

        // Assert
        result.State.Should().Be("REGISTRATION_PENDING");
        _challenges.Verify(x => x.ConsumeAsync("registration-verify", "challenge", It.IsAny<CancellationToken>()), Times.Once);
    }

    private RegistrationService CreateRegistrationService() => new(_challenges.Object, _data.Object, _email.Object, _passwords.Object,
        Microsoft.Extensions.Options.Options.Create(new AuthOptions
        {
            Credentials = new CredentialAuthOptions
            {
                RegistrationVerificationTtl = TimeSpan.FromMinutes(15),
                RegistrationCompletionTtl = TimeSpan.FromMinutes(10)
            }
        }), _environment.Object, _logger.Object, new Mock<ICurrentRequestContext>().Object);

    private AuthenticationService CreateService(string? pendingToken = null)
    {
        var context = new DefaultHttpContext();
        if (pendingToken != null)
        {
            context.Request.Headers.Cookie = $"pending={pendingToken}";
        }
        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };
        var request = new Mock<ICurrentRequestContext>();
        request.SetupGet(value => value.RemoteIpAddress).Returns("127.0.0.1");
        var options = Microsoft.Extensions.Options.Options.Create(new AuthOptions
        {
            Credentials = new CredentialAuthOptions
            {
                RoleSelectionTtl = TimeSpan.FromMinutes(5)
            },
            PendingSession = new PendingSessionAuthOptions
            {
                Cookie = new AuthCookieOptions
                {
                    Name = "pending"
                }
            }
        });
        return new AuthenticationService(_challenges.Object, _data.Object, _passwords.Object, options, accessor,
            new SessionCookieService(null!, null!, options), new Mock<ICurrentUserContext>().Object, request.Object, new AuthenticationSessionService(null!, accessor, null!), new AuthenticationEventLogger(request.Object, _logger.Object));
    }
}
