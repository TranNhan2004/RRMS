using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RRMS.Api.Controllers.V1;
using RRMS.Api.Dtos.V1.Users;
using RRMS.Application.Interfaces.Services.Users;
using RRMS.Application.Models.Users;
using Xunit;

namespace RRMS.Api.Tests.Controllers.V1;

public class UserControllerTests
{
    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUserAndProfile_WhenUserExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var service = new Mock<IUserQueryService>();
        service.Setup(x => x.GetCurrentUserAsync(CancellationToken.None)).ReturnsAsync(new GetCurrentUserResponseModel
        {
            Id = id,
            UserName = "alice",
            Email = "alice@example.com",
            Profile = new GetCurrentUserProfileResponseModel
            {
                FirstName = "Alice"
            }
        });
        var controller = new UserController(service.Object);

        // Act
        var result = await controller.GetCurrentUserAsync();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<GetCurrentUserResponse>().Subject;
        response.Id.Should().Be(id);
        response.Email.Should().Be("alice@example.com");
        response.Profile!.FirstName.Should().Be("Alice");
        service.Verify(x => x.GetCurrentUserAsync(CancellationToken.None), Times.Once);
    }
}
