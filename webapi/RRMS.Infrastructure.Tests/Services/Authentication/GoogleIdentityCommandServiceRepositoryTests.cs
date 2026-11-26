using AwesomeAssertions;
using Moq;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class GoogleIdentityCommandServiceRepositoryTests
{
    [Fact]
    public async Task LinkAsync_ShouldAddIdentityAndCommit_WhenVerifiedIdentityMatchesUser()
    {
        // Arrange
        var id = Guid.NewGuid();
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = id,
            Email = "user@example.org",
            Status = UserStatus.Active,
            IsEmailVerified = true
        });
        var identities = new Mock<IUserIdentityRepository>();
        var unit = new Mock<IUnitOfWork>();
        var service = new GoogleIdentityCommandService(users.Object, identities.Object, unit.Object);

        // Act
        await service.LinkAsync(id, "subject", "USER@example.org", true);

        // Assert
        identities.Verify(repository => repository.Add(It.Is<UserIdentity>(value => value.UserId == id && value.ProviderSubject == "subject")), Times.Once);
        unit.Verify(value => value.SaveChangesAsync(id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LinkAsync_ShouldNotCommit_WhenIdentityIsAlreadyLinked()
    {
        // Arrange
        var id = Guid.NewGuid();
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = id,
            Email = "user@example.org",
            Status = UserStatus.Active,
            IsEmailVerified = true
        });
        var identities = new Mock<IUserIdentityRepository>();
        identities.Setup(repository => repository.FindGoogleLinkExistsAsync(id, "subject", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var unit = new Mock<IUnitOfWork>();
        var service = new GoogleIdentityCommandService(users.Object, identities.Object, unit.Object);

        // Act
        var action = () => service.LinkAsync(id, "subject", "user@example.org", true);

        // Assert
        await action.Should().ThrowAsync<ConflictException>();
        identities.Verify(repository => repository.Add(It.IsAny<UserIdentity>()), Times.Never);
        unit.Verify(value => value.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
