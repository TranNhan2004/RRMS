using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class SessionManagementServiceTests
{
    [Fact]
    public async Task SwitchRoleAsync_ShouldRejectRole_WhenUserIsNotEligible()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Mock<ICurrentUserContext>();
        user.Setup(value => value.GetRequiredUserId()).Returns(userId);
        var data = new Mock<IAuthenticationDataService>();
        data.Setup(value => value.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        var service = new SessionManagementService(user.Object, new HttpContextAccessor(), data.Object, null!, null!, null!);

        // Act
        var action = () => service.SwitchRoleAsync("Admin", CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SwitchRoleAsync_ShouldDoNothing_WhenRoleIsAlreadyActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Mock<ICurrentUserContext>();
        user.Setup(value => value.GetRequiredUserId()).Returns(userId);
        user.SetupGet(value => value.ActiveRole).Returns("Tenant");
        var data = new Mock<IAuthenticationDataService>();
        data.Setup(value => value.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        var service = new SessionManagementService(user.Object, new HttpContextAccessor(), data.Object, null!, null!, null!);

        // Act
        await service.SwitchRoleAsync("Tenant", CancellationToken.None);

        // Assert
        data.Verify(value => value.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
