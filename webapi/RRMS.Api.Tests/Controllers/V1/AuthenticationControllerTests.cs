using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Api.Controllers.V1;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Infrastructure.Options;
using Xunit;
using IAuthenticationService = RRMS.Application.Interfaces.Services.Authentication.IAuthenticationService;

namespace RRMS.Api.Tests.Controllers.V1;

public class AuthenticationControllerTests
{
    [Fact]
    public async Task GetPendingSessionAsync_ShouldReturnServiceResult_WhenPendingSessionIsAvailable()
    {
        // Arrange
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetPendingSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationResultResponseModel());
        var controller = new AuthenticationController(authentication.Object, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers.Cookie = "__Host-custom-pending=selected-token; __Host-pending-session=old-token";

        // Act
        var result = await controller.GetPendingSessionAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        authentication.Verify(service => service.GetPendingSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
