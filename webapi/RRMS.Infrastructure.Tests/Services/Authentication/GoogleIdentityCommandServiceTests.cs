using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Persistence;
using RRMS.Infrastructure.Repositories;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class GoogleIdentityCommandServiceTests
{
    [Fact]
    public async Task LinkAsync_ShouldLinkIdentity_WhenAccountAndVerifiedGoogleEmailMatch()
    {
        // Arrange
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var unit = new Mock<IUnitOfWork>();
        var service = CreateService(context, unit.Object);

        // Act
        await service.LinkAsync(user.Id, "google-sub", "USER@example.org", true);

        // Assert
        context.ChangeTracker.Entries<UserIdentity>().Should().ContainSingle(entry => entry.Entity.UserId == user.Id && entry.Entity.ProviderSubject == "google-sub" && entry.Entity.Provider == UserIdentityProvider.Google);
        unit.Verify(x => x.SaveChangesAsync(user.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("other@example.org", true)]
    [InlineData("user@example.org", false)]
    public async Task LinkAsync_ShouldRejectLink_WhenGoogleEmailIsUnverifiedOrDifferent(string email, bool verified)
    {
        // Arrange
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var unit = new Mock<IUnitOfWork>();
        var service = CreateService(context, unit.Object);

        // Act
        var action = () => service.LinkAsync(user.Id, "google-sub", email, verified);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkAsync_ShouldRejectLink_WhenGoogleIdentityIsAlreadyAssigned()
    {
        // Arrange
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        context.UserIdentities.Add(new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Provider = UserIdentityProvider.Google,
            ProviderSubject = "google-sub"
        });
        await context.SaveChangesAsync();
        var unit = new Mock<IUnitOfWork>();
        var service = CreateService(context, unit.Object);

        // Act
        var action = () => service.LinkAsync(user.Id, "google-sub", user.Email, true);

        // Assert
        await action.Should().ThrowAsync<ConflictException>();
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static GoogleIdentityCommandService CreateService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        var logger = new Mock<Serilog.ILogger>().Object;
        return new GoogleIdentityCommandService(new UserRepository(logger, context), new UserIdentityRepository(logger, context), unitOfWork);
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<User> AddUserAsync(AppDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.org",
            Status = UserStatus.Active,
            IsEmailVerified = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
