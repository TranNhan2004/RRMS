using AwesomeAssertions;
using RRMS.Domain.Constants.Uam;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class RoleCodesTests
{
    [Fact]
    public void RoleCodes_ShouldUsePascalCaseNames_WhenRepresentingDefaultRoles()
    {
        // Arrange
        const string expectedAdmin = "Admin";
        const string expectedLandlord = "Landlord";
        const string expectedTenant = "Tenant";

        // Act
        var admin = RoleCodes.Admin;
        var landlord = RoleCodes.Landlord;
        var tenant = RoleCodes.Tenant;

        // Assert
        admin.Should().Be(expectedAdmin);
        landlord.Should().Be(expectedLandlord);
        tenant.Should().Be(expectedTenant);
    }
}
