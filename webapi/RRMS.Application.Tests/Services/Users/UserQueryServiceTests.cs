using AwesomeAssertions;
using Moq;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Cache;
using RRMS.Application.Models.Users;
using RRMS.Application.Services.Users;
using RRMS.Domain.Constants.CacheKeys;
using RRMS.Domain.Exceptions;
using Xunit;

namespace RRMS.Application.Tests.Services.Users;

public class UserQueryServiceTests
{
    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnCachedUser_WhenCacheContainsUser()
    {
        // Arrange
        var id = Guid.NewGuid();
        var context = new Mock<ICurrentUserContext>();
        context.Setup(x => x.GetRequiredUserId()).Returns(id);
        var users = new Mock<IUserRepository>();
        var cache = new Mock<ICacheService>();
        var expected = new GetCurrentUserResponseModel
        {
            Id = id,
            Email = "test@example.org"
        };
        cache.Setup(x => x.GetAsync<GetCurrentUserResponseModel>(It.IsAny<CacheKeyObject>(), CancellationToken.None)).ReturnsAsync(expected);
        var service = new UserQueryService(context.Object, users.Object, cache.Object);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        result.Should().BeSameAs(expected);
        users.Verify(x => x.FindCurrentUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(x => x.SetAsync(It.IsAny<CacheKeyObject>(), It.IsAny<GetCurrentUserResponseModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldQueryAndCacheUser_WhenCacheMisses()
    {
        // Arrange
        var id = Guid.NewGuid();
        var context = new Mock<ICurrentUserContext>();
        context.Setup(x => x.GetRequiredUserId()).Returns(id);
        var users = new Mock<IUserRepository>();
        var cache = new Mock<ICacheService>();
        var expected = new GetCurrentUserResponseModel
        {
            Id = id,
            Email = "test@example.org"
        };
        users.Setup(x => x.FindCurrentUserAsync(id, CancellationToken.None)).ReturnsAsync(expected);
        var service = new UserQueryService(context.Object, users.Object, cache.Object);

        // Act
        var result = await service.GetCurrentUserAsync();

        // Assert
        result.Should().BeSameAs(expected);
        cache.Verify(x => x.SetAsync(It.Is<CacheKeyObject>(key => key.Key == $"rrms:users:current:v2:{id}"), expected, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var context = new Mock<ICurrentUserContext>();
        context.Setup(x => x.GetRequiredUserId()).Returns(Guid.NewGuid());
        var users = new Mock<IUserRepository>();
        var cache = new Mock<ICacheService>();
        var service = new UserQueryService(context.Object, users.Object, cache.Object);

        // Act
        var action = () => service.GetCurrentUserAsync();

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
        cache.Verify(x => x.SetAsync(It.IsAny<CacheKeyObject>(), It.IsAny<GetCurrentUserResponseModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
