using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Exceptions;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Contexts;
using Xunit;

namespace RRMS.Infrastructure.Tests.Contexts;

public class CurrentUserContextTests
{
    [Fact]
    public void UserId_ShouldReturnSubject_WhenPrincipalIsAuthenticated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Bearer"));
        var currentUser = CreateContext(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        // Act
        var result = currentUser.UserId;

        // Assert
        result.Should().Be(id);
    }

    [Fact]
    public void UserId_ShouldReflectCurrentPrincipal_WhenIdentityChanges()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var currentUser = CreateContext(new HttpContextAccessor
        {
            HttpContext = context
        });

        // Act
        var initialUserId = currentUser.UserId;
        var id = Guid.NewGuid();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Session"));
        var authenticatedUserId = currentUser.GetRequiredUserId();

        // Assert
        initialUserId.Should().BeNull();
        authenticatedUserId.Should().Be(id);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("invalid")]
    public void GetRequiredUserId_ShouldRejectIdentifier_WhenSubjectIsInvalid(string subject)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "Session"));
        var currentUser = CreateContext(new HttpContextAccessor
        {
            HttpContext = context
        });

        // Act
        var action = () => currentUser.GetRequiredUserId();

        // Assert
        action.Should().Throw<UnauthorizedException>();
    }

    [Fact]
    public void UserId_ShouldRejectSubject_WhenPrincipalIsAnonymous()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())]));
        var currentUser = CreateContext(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        // Act
        var userId = currentUser.UserId;
        var action = () => currentUser.GetRequiredUserId();

        // Assert
        userId.Should().BeNull();
        action.Should().Throw<UnauthorizedException>();
    }
    [Fact]
    public void GetRequiredUserId_ShouldReturnConfiguredUser_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userRepository = new Mock<IUserRepository>();
        var candidates = new[]
        {
            new User { Id = Guid.NewGuid(), Email = "other@example.org" },
            new User { Id = id, Email = "USER@example.org" }
        }.AsQueryable();
        userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Func<IQueryable<User>, IQueryable<User>> filter, bool _, CancellationToken _) => filter(candidates).FirstOrDefault());
        var currentUser = CreateContext(new HttpContextAccessor(), "Development", " User@Example.org ", userRepository);

        // Act
        var result = currentUser.GetRequiredUserId();

        // Assert
        result.Should().Be(id);
        userRepository.Verify(repository => repository.FindFirstByConditionAsync(
            It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetRequiredUserId_ShouldRejectRequest_WhenDevelopmentBypassUserDoesNotExist()
    {
        // Arrange
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var currentUser = CreateContext(new HttpContextAccessor(), "Development", "missing@example.org", userRepository);

        // Act
        var action = () => currentUser.GetRequiredUserId();

        // Assert
        action.Should().Throw<UnauthorizedException>();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Dev")]
    [InlineData("Test")]
    public void GetRequiredUserId_ShouldUseAuthenticatedUser_WhenEnvironmentIsNotDevelopment(string environment)
    {
        // Arrange
        var id = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Session"));
        var userRepository = new Mock<IUserRepository>();
        var currentUser = CreateContext(new HttpContextAccessor { HttpContext = context }, environment, "user@example.org", userRepository);

        // Act
        var result = currentUser.GetRequiredUserId();

        // Assert
        result.Should().Be(id);
        userRepository.Verify(repository => repository.FindFirstByConditionAsync(
            It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetRequiredUserId_ShouldRejectRequest_WhenDevelopmentBypassUserHasEmptyId()
    {
        // Arrange
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.FindFirstByConditionAsync(
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.Empty, Email = "user@example.org" });
        var currentUser = CreateContext(new HttpContextAccessor(), "Development", "user@example.org", userRepository);

        // Act
        var action = () => currentUser.GetRequiredUserId();

        // Assert
        action.Should().Throw<UnauthorizedException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetRequiredUserId_ShouldUseAuthenticatedUser_WhenDevelopmentBypassEmailIsMissing(string? email)
    {
        // Arrange
        var id = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Session"));
        var userRepository = new Mock<IUserRepository>();
        var currentUser = CreateContext(new HttpContextAccessor { HttpContext = context }, "Development", email, userRepository);

        // Act
        var result = currentUser.GetRequiredUserId();

        // Assert
        result.Should().Be(id);
        userRepository.Verify(repository => repository.FindFirstByConditionAsync(
            It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CurrentUserContext CreateContext(IHttpContextAccessor accessor, string environment = "Production", string? email = null, Mock<IUserRepository>? userRepository = null)
    {
        var host = new Mock<IHostEnvironment>();
        host.SetupGet(value => value.EnvironmentName).Returns(environment);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LocalBypassUserEmail"] = email
        }).Build();
        return new CurrentUserContext(accessor, host.Object, configuration, (userRepository ?? new Mock<IUserRepository>()).Object);
    }
}
