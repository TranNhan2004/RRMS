using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;
using RRMS.Domain.Enums.Users;
using RRMS.Infrastructure.Persistence;
using RRMS.Infrastructure.Repositories;
using Xunit;

namespace RRMS.Infrastructure.Tests.Repositories;

public class AuthenticationRepositoryTests
{
    [Fact]
    public async Task FindByIdentityAsync_ShouldReturnAtMostTwoMatchingCredentials_WhenIdentityIsAmbiguous()
    {
        // Arrange
        await using var context = CreateContext();
        for (var index = 0; index < 3; index++)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"user{index}@example.org",
                UserName = "Shared"
            };
            context.Users.Add(user);
            context.UserCredentials.Add(new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PasswordHash = "hash"
            });
        }
        await context.SaveChangesAsync();
        var repository = new UserCredentialRepository(new Mock<Serilog.ILogger>().Object, context);

        // Act
        var result = await repository.FindByIdentityAsync("shared");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(credential => credential.User.UserName == "Shared");
    }

    [Fact]
    public async Task FindEligibleRoleCodesAsync_ShouldReturnDistinctRoles_WhenUserIsActiveAndVerified()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.org",
            Status = UserStatus.Active,
            IsEmailVerified = true
        };
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Code = "Tenant",
            DisplayName = "Tenant"
        };
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id
        });
        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id
        });
        await context.SaveChangesAsync();
        var repository = new UserRoleRepository(new Mock<Serilog.ILogger>().Object, context);

        // Act
        var result = await repository.FindEligibleRoleCodesAsync(user.Id);

        // Assert
        result.Should().Equal("Tenant");

        // Arrange
        user.IsEmailVerified = false;
        await context.SaveChangesAsync();

        // Act
        var unverifiedRoles = await repository.FindEligibleRoleCodesAsync(user.Id);

        // Assert
        unverifiedRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task FindEmailExistsAsync_ShouldIncludeDeletedUsers_WhenCheckingEmailReuse()
    {
        // Arrange
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "USER@example.org",
            IsDeleted = true
        });
        await context.SaveChangesAsync();
        var repository = new UserRepository(new Mock<Serilog.ILogger>().Object, context);

        // Act
        var exists = await repository.FindEmailExistsAsync("user@example.org");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FindGoogleLinkExistsAsync_ShouldMatchUserOrProviderSubject_WhenIdentityIsAlreadyLinked()
    {
        // Arrange
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        context.UserIdentities.Add(new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = UserIdentityProvider.Google,
            ProviderSubject = "subject"
        });
        await context.SaveChangesAsync();
        var repository = new UserIdentityRepository(new Mock<Serilog.ILogger>().Object, context);

        // Act
        var foundUserId = await repository.FindGoogleUserIdAsync("subject");
        var existingUser = await repository.FindGoogleLinkExistsAsync(userId, "another-subject");
        var existingSubject = await repository.FindGoogleLinkExistsAsync(Guid.NewGuid(), "subject");
        var missingLink = await repository.FindGoogleLinkExistsAsync(Guid.NewGuid(), "another-subject");

        // Assert
        foundUserId.Should().Be(userId);
        existingUser.Should().BeTrue();
        existingSubject.Should().BeTrue();
        missingLink.Should().BeFalse();
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
