using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Constants.Uam;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Persistence;
using RRMS.Infrastructure.Repositories;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class GoogleIdentityResolutionTests
{
    [Fact]
    public async Task ResolveGoogleUserAsync_ShouldCreateActiveVerifiedUser_WhenIdentityAndEmailAreNew()
    {
        // Arrange
        await using var context = CreateContext();
        context.Roles.Add(new Role
        {
            Id = Guid.NewGuid(),
            Code = RoleCodes.Tenant,
            DisplayName = "Tenant"
        });
        await context.SaveChangesAsync();
        var unit = new Mock<IUnitOfWork>();
        var service = CreateService(context, unit.Object);

        // Act
        var id = await service.ResolveGoogleUserAsync("google-sub", "New@Example.org", true);

        // Assert
        var user = context.ChangeTracker.Entries<User>().Single(entry => entry.Entity.Id == id).Entity;
        user.Status.Should().Be(UserStatus.Active);
        user.IsEmailVerified.Should().BeTrue();
        user.Email.Should().Be("new@example.org");
        context.ChangeTracker.Entries<UserIdentity>().Should().ContainSingle(entry => entry.Entity.UserId == id && entry.Entity.ProviderSubject == "google-sub" && entry.Entity.Provider == UserIdentityProvider.Google);
        context.ChangeTracker.Entries<UserRole>().Should().ContainSingle(entry => entry.Entity.UserId == id);
        unit.Verify(x => x.SaveChangesAsync(id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveGoogleUserAsync_ShouldRejectAutomaticLink_WhenEmailBelongsToExistingUser()
    {
        // Arrange
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.org",
            Status = UserStatus.Active,
            IsEmailVerified = true
        });
        await context.SaveChangesAsync();
        var unit = new Mock<IUnitOfWork>();
        var service = CreateService(context, unit.Object);

        // Act
        var action = () => service.ResolveGoogleUserAsync("unknown-sub", "USER@example.org", true);

        // Assert
        await action.Should().ThrowAsync<ConflictException>();
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveGoogleUserAsync_ShouldRejectNewUser_WhenEmailIsUnverified()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context, new Mock<IUnitOfWork>().Object);

        // Act
        var action = () => service.ResolveGoogleUserAsync("unknown-sub", "user@example.org", false);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    private static AuthenticationDataService CreateService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        var logger = new Mock<Serilog.ILogger>().Object;
        return new AuthenticationDataService(
            new UserRepository(logger, context),
            new UserCredentialRepository(logger, context),
            new UserProfileRepository(logger, context),
            new UserIdentityRepository(logger, context),
            new UserRoleRepository(logger, context),
            new RoleRepository(logger, context),
            unitOfWork);
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
