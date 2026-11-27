using AwesomeAssertions;
using Moq;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Constants.Uam;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class AuthenticationDataServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserCredentialRepository> _credentials = new();
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IUserIdentityRepository> _identities = new();
    private readonly Mock<IUserRoleRepository> _userRoles = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task CreateVerifiedUserAsync_ShouldAddUserAndRelatedRecords_WhenEmailIsAvailable()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _roles.Setup(repository => repository.FindByCodeAsync(RoleCodes.Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role
            {
                Id = roleId
            });
        User? createdUser = null;
        _users.Setup(repository => repository.Add(It.IsAny<User>()))
            .Callback<User>(user => createdUser = user);

        // Act
        await CreateService().CreateVerifiedUserAsync("user@example.org", "password-hash");

        // Assert
        createdUser.Should().NotBeNull();
        _credentials.Verify(repository => repository.Add(It.Is<UserCredential>(value => value.UserId == createdUser!.Id && value.PasswordHash == "password-hash")), Times.Once);
        _profiles.Verify(repository => repository.Add(It.Is<UserProfile>(value => value.UserId == createdUser!.Id)), Times.Once);
        _userRoles.Verify(repository => repository.Add(It.Is<UserRole>(value => value.UserId == createdUser!.Id && value.RoleId == roleId)), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(createdUser!.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateVerifiedUserAsync_ShouldNotWrite_WhenEmailAlreadyExists()
    {
        // Arrange
        _users.Setup(repository => repository.FindEmailExistsAsync("user@example.org", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var action = () => CreateService().CreateVerifiedUserAsync("user@example.org", "hash");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        _users.Verify(repository => repository.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateVerifiedUserAsync_ShouldNotWrite_WhenDefaultRoleIsMissing()
    {
        // Arrange
        var service = CreateService();

        // Act
        var action = () => service.CreateVerifiedUserAsync("user@example.org", "hash");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        _users.Verify(repository => repository.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveGoogleUserAsync_ShouldReturnExistingUser_WhenSubjectIsLinked()
    {
        // Arrange
        var id = Guid.NewGuid();
        _identities.Setup(repository => repository.FindGoogleUserIdAsync("subject", It.IsAny<CancellationToken>())).ReturnsAsync(id);

        // Act
        var result = await CreateService().ResolveGoogleUserAsync("subject", null, false);

        // Assert
        result.Should().Be(id);
        _users.Verify(repository => repository.FindEmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveGoogleUserAsync_ShouldCreateRelatedIdentity_WhenSubjectAndEmailAreNew()
    {
        // Arrange
        _roles.Setup(repository => repository.FindByCodeAsync(RoleCodes.Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role
            {
                Id = Guid.NewGuid()
            });

        // Act
        var id = await CreateService().ResolveGoogleUserAsync("subject", "USER@example.org", true);

        // Assert
        _users.Verify(repository => repository.Add(It.Is<User>(value => value.Id == id && value.Email == "user@example.org" && value.IsEmailVerified)), Times.Once);
        _identities.Verify(repository => repository.Add(It.Is<UserIdentity>(value => value.UserId == id && value.Provider == UserIdentityProvider.Google && value.ProviderSubject == "subject")), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveGoogleUserAsync_ShouldRejectLink_WhenEmailBelongsToExistingUser()
    {
        // Arrange
        _users.Setup(repository => repository.FindEmailExistsAsync("user@example.org", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var action = () => CreateService().ResolveGoogleUserAsync("subject", "USER@example.org", true);

        // Assert
        await action.Should().ThrowAsync<ConflictException>();
        _identities.Verify(repository => repository.Add(It.IsAny<UserIdentity>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private AuthenticationDataService CreateService() => new(
        _users.Object,
        _credentials.Object,
        _profiles.Object,
        _identities.Object,
        _userRoles.Object,
        _roles.Object,
        _unitOfWork.Object);
}
