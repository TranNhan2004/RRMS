using AwesomeAssertions;
using Moq;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Cache;
using RRMS.Application.Models.Users;
using RRMS.Application.Services.Users;
using RRMS.Domain.Constants.CacheKeys;
using RRMS.Domain.Entities;
using RRMS.Domain.Exceptions;
using Xunit;

namespace RRMS.Application.Tests.Services.Users;

public class UserCommandServiceTests
{
    [Fact]
    public async Task SetUserNameAsync_ShouldSetUserNameAndInvalidateCache_WhenUserNameIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId
        };
        var userRepository = CreateRepository(user);
        var unitOfWork = new Mock<IUnitOfWork>();
        var cacheService = new Mock<ICacheService>();
        var service = CreateService(userId, userRepository, unitOfWork, cacheService);
        var request = new SetUserNameRequestModel
        {
            UserName = " Alice "
        };

        // Act
        var result = await service.SetUserNameAsync(request);

        // Assert
        result.UserName.Should().Be("Alice");
        user.UserName.Should().Be("Alice");
        unitOfWork.Verify(value => value.SaveChangesAsync(userId.ToString(), CancellationToken.None), Times.Once);
        cacheService.Verify(value => value.RemoveAsync(It.Is<CacheKeyObject>(key => key.Key == UserCacheKeys.GetCurrentUserKey(userId.ToString()).Key), CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("")]
    public async Task SetUserNameAsync_ShouldRejectRequest_WhenUserNameIsAlreadySet(string existingName)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userRepository = CreateRepository(new User
        {
            Id = userId,
            UserName = existingName
        });
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = CreateService(userId, userRepository, unitOfWork, new Mock<ICacheService>());

        // Act
        var action = () => service.SetUserNameAsync(new SetUserNameRequestModel
        {
            UserName = "new-name"
        });

        // Assert
        await action.Should().ThrowAsync<UnprocessableEntityException>();
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetUserNameAsync_ShouldRejectRequest_WhenUserDoesNotExist()
    {
        // Arrange
        var service = CreateService(Guid.NewGuid(), CreateRepository(null), new Mock<IUnitOfWork>(), new Mock<ICacheService>());

        // Act
        var action = () => service.SetUserNameAsync(new SetUserNameRequestModel
        {
            UserName = "alice"
        });

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetUserNameAsync_ShouldRejectRequest_WhenUserNameIsBlank(string userName)
    {
        // Arrange
        var userRepository = new Mock<IUserRepository>();
        var service = CreateService(Guid.NewGuid(), userRepository, new Mock<IUnitOfWork>(), new Mock<ICacheService>());

        // Act
        var action = () => service.SetUserNameAsync(new SetUserNameRequestModel
        {
            UserName = userName
        });

        // Assert
        await action.Should().ThrowAsync<UnprocessableEntityException>();
        userRepository.Verify(value => value.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IUserRepository> CreateRepository(User? user)
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(value => value.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<User>, IQueryable<User>>>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return userRepository;
    }

    private static UserCommandService CreateService(Guid userId, Mock<IUserRepository> userRepository, Mock<IUnitOfWork> unitOfWork, Mock<ICacheService> cacheService)
    {
        var currentUserContext = new Mock<ICurrentUserContext>();
        currentUserContext.Setup(value => value.GetRequiredUserId()).Returns(userId);
        return new UserCommandService(currentUserContext.Object, userRepository.Object, unitOfWork.Object, cacheService.Object);
    }
}
